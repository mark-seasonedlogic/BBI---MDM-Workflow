using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Kernel;
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using System.Threading;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public class WS1BatteryDrainViewModel : INotifyPropertyChanged
    {
        public string PageDisplay => $"{CurrentPage} / {TotalPages}";
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        private void NotifyPaginationChanged()
        {
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageDisplay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageDisplay)); // If you're using a display string
                    OnPropertyChanged(nameof(CanGoPrevious));
                    OnPropertyChanged(nameof(CanGoNext));
                }
            }
        }

        public int PageSize { get; private set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)restaurantDrainData.Count / PageSize);

        private List<RestaurantDrainEntry> restaurantDrainData = new();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ISeries[] _series;
        public ISeries[] Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged();
            }
        }

        private IEnumerable<ICartesianAxis> _xAxes;
        public IEnumerable<ICartesianAxis> XAxes
        {
            get => _xAxes;
            set
            {
                _xAxes = value;
                OnPropertyChanged();
            }
        }

        private IEnumerable<ICartesianAxis> _yAxes;
        public IEnumerable<ICartesianAxis> YAxes {
            get => _yAxes;
            set
            {
                _yAxes = value;
                OnPropertyChanged();
            }
        }
        public Func<ChartPoint, string> TooltipFormatter { get; set; }
        
        private string[] restaurantNames;
        public string[] RestaurantNames => restaurantNames;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private RestaurantMasterHelper restaurantMasterHelper = new RestaurantMasterHelper(Logger);
        private readonly IDictionary<string, RestaurantMasterInfo> restaurantCorpInfo;
        private readonly IDictionary<string, RestaurantMasterInfo> restaurantNonCorpInfo;
        public async Task LoadBatteryFilesAsync(string folderPath)
        {
            var analyzer = new BatteryDrainAnalyzer(Logger);
            var heuristicDrainResults = new List<ShiftDrainResult>();
            var rapidDrainDevices = new List<RapidDrainEvent>();

            Logger.Info($"Loading battery snapshots from folder: {folderPath}");
            var files = Directory.GetFiles(folderPath, "Run*.json");
            Logger.Info($"Found {files.Length} snapshot files.");

            int fileCounter = 1;
            var allDevices = files
                .AsParallel()
                .WithDegreeOfParallelism(5)
                .SelectMany(file =>
                {
                    int currentCount = Interlocked.Increment(ref fileCounter);
                    Logger.Info($"Processing file #{currentCount}: {Path.GetFileName(file)}");

                    try
                    {
                        var json = File.ReadAllText(file);
                        var devices = JsonSerializer.Deserialize<List<DeviceSnapshot>>(json);

                        return devices?
                            .Where(d => !string.IsNullOrEmpty(d.SerialNumber) && !string.IsNullOrEmpty(d.UserName))
                            .Where(d => d.LastSeen.HasValue && (DateTime.UtcNow - d.LastSeen.Value).TotalDays <= 17)
                            .Select(d =>
                            {
                                var battery = d.CustomAttributes?
                                    .FirstOrDefault(attr => attr.Name == "miscellaneous.batteryLevel")
                                    ?.GetBatteryValue();
                                var model = d.CustomAttributes?
                                    .FirstOrDefault(attr => attr.Name == "identity.deviceModel")?.Value ?? "UNKNOWN";

                                if (battery is null) return null;
                                var restaurantCode = d.UserName.Substring(0, 7);
                                var restaurantDetails = new RestaurantMasterInfo();
                                if (!restaurantCorpInfo.TryGetValue(restaurantCode, out restaurantDetails) && !restaurantNonCorpInfo.TryGetValue(restaurantCode, out restaurantDetails))
                                {
                                    Logger.Warn($"Timezone not found for restaurant {restaurantCode}, defaulting to EST.");
                                    restaurantDetails = new RestaurantMasterInfo { TimeZoneName = "Eastern Standard Time" };
                                }

                                var lastSeenLocalTime = DateTimeConversionHelper.ConvertToDeviceLocalTime(
                                    d.LastSeen.Value,
                                    restaurantDetails.TimeZoneName
                                );
                                var enrollmentLocalTime = DateTimeConversionHelper.ConvertToDeviceLocalTime(
                                    d.EnrollmentDate.Value,
                                    restaurantDetails.TimeZoneName
                                );

                                return new BatterySnapshot(
                                    d.SerialNumber,
                                    lastSeenLocalTime,
                                    battery.Value,
                                    d.UserName.Substring(0, 7),
                                    model,
                                    enrollmentLocalTime
                                );
                            })
                            .Where(x => x != null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to parse file {file}");
                        return Enumerable.Empty<BatterySnapshot>();
                    }
                })
                .ToList();

            Logger.Info($"Parsed {allDevices.Count} valid battery entries.");

            foreach (var deviceGroup in allDevices.GroupBy(d => d.SerialNumber))
            {
                if (deviceGroup.Count() < 2) continue;

                var ordered = deviceGroup
                    .GroupBy(x => x.Timestamp)
                    .Select(g => g.First())
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                if (!ordered.Any()) continue;

                var serial = ordered[0].SerialNumber;
                var restaurant = ordered[0].Restaurant;
                var model = ordered[0].DeviceModel;
                // ✅ New: Analyze each day separately for shifts
                foreach (var dayGroup in ordered.GroupBy(x => x.Timestamp.Date))
                {
                    // Analyze lunch shift (11am–4pm)
                    var lunchResults = analyzer.AnalyzeShiftDrain(
                    serial, restaurant, model, ordered,
                    shiftStart: new TimeSpan(10, 0, 0),
                    shiftEnd: new TimeSpan(16, 0, 0),
                    shiftName: "Lunch",
                    ordered[0].EnrollmentDateTime);

                    // Analyze dinner shift (5pm–10pm)
                    var dinnerResults = analyzer.AnalyzeShiftDrain(
                        serial, restaurant, model, ordered,
                        shiftStart: new TimeSpan(16, 1, 0),
                        shiftEnd: new TimeSpan(23, 0, 0),
                        shiftName: "Dinner",
                        ordered[0].EnrollmentDateTime);

                    heuristicDrainResults.AddRange(lunchResults);
                    heuristicDrainResults.AddRange(dinnerResults);
                }
                var rapidEvents = analyzer.DetectRapidDrainEvents(serial, restaurant, model, ordered[0].EnrollmentDateTime, ordered);
                rapidDrainDevices.AddRange(rapidEvents);

            }



            // Step 1: Aggregate restaurant-level results from heuristic shift drain analysis
            var groupedByRestaurant = heuristicDrainResults
                .GroupBy(x => x.Restaurant)
                .Select(g => new RestaurantDrainEntry
                {
                    Restaurant = g.Key,
                    DeviceCount = g.Count(),
                    AvgDrop = g.Average(x => x.Drain),
                    AvgHours = g.Average(x => x.Hours),
                    AvgDropPerHour = g.Average(x => x.DropPerHour),
                    DeviceModels = string.Join(", ", g.Select(x => x.Model).Distinct())
                })
                .OrderByDescending(x => x.AvgDrop)
                .ToList();
            // Write out raw data
            var rawCsvPath = Path.Combine("logs", "RawHeuristicDrainResults.csv");
            using (var writer = new StreamWriter(rawCsvPath))
            {/*
                    public class ShiftDrainResult
    {
        public string SerialNumber { get; set; }
        public string Restaurant { get; set; }
        public string Model { get; set; }
        public string ShiftName { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public DateTime StartTimestamp { get; set; }
        public DateTime EndTimestamp { get; set; }
        public double StartBattery { get; set; }
        public double EndBattery { get; set; }
        public double Drain { get; set; }
        public double Hours { get; set; }
        public double DropPerHour { get; set; }
        public DateTime FirstSampleTime { get; set; }
        public DateTime LastSampleTime { get; set; }
    }

             */
                writer.WriteLine("Restaurant,SerialNumber,Model,EnrollmentDate,Shift,FirstSampleTime,LastSampleTime,Drain(%),Hours,DropPerHour");

                foreach (var result in heuristicDrainResults)
                {
                    writer.WriteLine($"{result.Restaurant},{result.SerialNumber},{result.Model},{result.EnrollmentDate},{result.ShiftName}," +
                                     $"{result.FirstSampleTime},{result.LastSampleTime},{result.Drain:F1},{result.Hours:F2},{result.DropPerHour:F2}");
                }
            }
            Logger.Info($"Wrote raw heuristic results CSV to: {rawCsvPath}");

            // Step 2: Update global view data for UI
            restaurantDrainData = groupedByRestaurant;
            CurrentPage = 1;
            ApplyPagination();
            NotifyPaginationChanged();

            // Step 3: Log summary per restaurant
            foreach (var r in groupedByRestaurant)
            {
                Logger.Info($"Restaurant: {r.Restaurant} | Devices: {r.DeviceCount} | Avg Drop: {r.AvgDrop:F1}% | Avg Hours: {r.AvgHours:F1} | Drop/Hr: {r.AvgDropPerHour:F2}");
            }

            // Step 4: Export summary CSV
            Directory.CreateDirectory("logs");
            var csvPath = Path.Combine("logs", "BatteryDrainSummary.csv");
            using (var writer = new StreamWriter(csvPath))
            {
                writer.WriteLine("Restaurant,DeviceCount,AvgDrop,AvgHours,AvgDropPerHour,DeviceModels");
                foreach (var r in groupedByRestaurant)
                {
                    writer.WriteLine($"{r.Restaurant},{r.DeviceCount},{r.AvgDrop:F1},{r.AvgHours:F1},{r.AvgDropPerHour:F2},\"{r.DeviceModels}\"");
                }
            }
            Logger.Info($"Wrote summary CSV to: {csvPath}");
            Logger.Info("Battery chart updated successfully.");

            // Step 5: Export heuristic-based rapid drain events
            var rapidCsvPath = Path.Combine("logs", "RapidDrainEvents.csv");
            using (var writer = new StreamWriter(rapidCsvPath))
            {
                writer.WriteLine("StartTime,EndTime,SerialNumber,EnrollmentDate,Restaurant,Model,Drain(%),Hours,DropPerHour");
                foreach (var d in rapidDrainDevices)
                {
                    writer.WriteLine($"{d.EventStart},{d.EventEnd},{d.SerialNumber},{d.EnrollmentDateTime},{d.Restaurant},{d.Model},{d.Drain:F1},{d.Hours:F2},{d.DropPerHour:F2}");
                }
            }
            Logger.Info($"Wrote rapid drain CSV to: {rapidCsvPath}");
        }

        public record RestaurantDrainEntry
        {
            public string Restaurant { get; set; }
            public double AvgDrop { get; set; }
            public int DeviceCount { get; set; }
            public double AvgDropPerHour { get; set; }
            public double AvgHours { get; set; }           
            public string DeviceModels { get; set; }       
        }

        public class DeviceSnapshot
        {
            public string UserName { get; set; }
            public DateTime? LastSeen { get; set; }
            public List<CustomAttribute> CustomAttributes { get; set; }
            public string SerialNumber { get; set; }
            public DateTime? EnrollmentDate { get; set; }

        }
        

        public class CustomAttribute
        {
            public string Name { get; set; }
            public string Value { get; set; }

            public double? GetBatteryValue()
            {
                if (Name != "miscellaneous.batteryLevel") return null;
                return double.TryParse(Value, out var result) ? result : null;
            }
        }
        public class DeviceDrainResult
        {
            public string Restaurant { get; set; }
            public double Drop { get; set; }
            public double Hours { get; set; }
            public double DropPerHour { get; set; }
            public string DeviceModel { get; set; }
        }

        public void ApplyPagination()
        {
            var paged = restaurantDrainData
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            restaurantNames = paged.Select(x => x.Restaurant).ToArray();

            // Create a new values array each time
            var values = paged.Select(x => x.AvgDrop).ToArray();

            // Recreate the full series with new references
            Series = new ISeries[]
            {
        new ColumnSeries<double>
        {
            Values = values,
            Name = "Battery Drain (%)"
        }
            };

            XAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Labels = restaurantNames,
            LabelsRotation = 15
        }
            };

            YAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Name = "Battery Drop (%)"
        }
            };

            TooltipFormatter = point =>
            {
                int index = point.Index;
                var value = Convert.ToDouble(point.Coordinate.PrimaryValue);
                var label = index < restaurantNames.Length ? restaurantNames[index] : $"Index {index}";
                return $"{label}: {value}%";
            };



            Logger.Info($"Displayed page {CurrentPage} of {TotalPages}");
        }

        public void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;

                ApplyPagination();
                NotifyPaginationChanged();
            }
        }

        public void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;

                ApplyPagination();
                NotifyPaginationChanged();
            }
        }

        public WS1BatteryDrainViewModel()
        {
            restaurantCorpInfo = restaurantMasterHelper.LoadCorpRestaurantTimeZones("C:\\Users\\MarkYoung\\Documents\\MarkYoungRestaurant20250227154702.csv");
            restaurantNonCorpInfo = restaurantMasterHelper.LoadNonCorpRestaurantTimeZones("C:\\Users\\MarkYoung\\Documents\\MarkYoungRestaurantNonCorp20250501124657.csv");
        }
        
        private string ToEstString(DateTime utcTime)
        {
            var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, est).ToString("g");
        }
        private DateTime ToEstDate(DateTime utcTime)
        {
            var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, est);
        }


    }
}
