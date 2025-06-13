using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Helpers
{
    public class ShiftDrainResult
    {
        public string SerialNumber { get; set; }
        public string Restaurant { get; set; }
        public DateTime EnrollmentDate { get; set; }
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


    public class BatteryDrainAnalyzer
    {
        private ILogger Logger;
        
        private DateTime ToEstDate(DateTime utcTime)
        {
            var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, est);
        }
        public enum USTimeZone
        {
            EST,
            CST,
            MST,
            PST
        }

        public List<RapidDrainEvent> DetectRapidDrainEvents(
    string serialNumber,
    string restaurant,
    string model,
    DateTime enrollmentDate,
    List<BatterySnapshot> orderedSnapshots,
    double drainPerHourThreshold = 20.0,
    double minDrainPercent = 10.0,
    double minDurationHours = 0.5)
        {
            var events = new List<RapidDrainEvent>();

            for (int i = 0; i < orderedSnapshots.Count - 1; i++)
            {
                var start = orderedSnapshots[i];
                for (int j = i + 1; j < orderedSnapshots.Count; j++)
                {
                    var end = orderedSnapshots[j];

                    var hours = (end.Timestamp - start.Timestamp).TotalHours;
                    if (hours < minDurationHours) continue;

                    var drain = start.Battery - end.Battery;
                    if (drain < minDrainPercent) continue;

                    var dropPerHour = drain / hours;
                    if (dropPerHour >= drainPerHourThreshold)
                    {
                        events.Add(new RapidDrainEvent
                        {
                            EventStart = ToEstDate(start.Timestamp),
                            EventEnd = ToEstDate(end.Timestamp),
                            SerialNumber = serialNumber,
                            Restaurant = restaurant,
                            Model = model,
                            EnrollmentDateTime = enrollmentDate,
                            Drain = drain,
                            Hours = hours,
                            DropPerHour = dropPerHour
                        });

                        i = j - 1; // Skip overlapping windows
                        break;
                    }
                }
            }

            return events;
        }


        public BatteryDrainAnalyzer(ILogger logger)
        {
            Logger = logger;
        }


        public List<ShiftDrainResult> AnalyzeShiftDrain(
    string serialNumber,
    string restaurant,
    string model,
    List<BatterySnapshot> orderedSnapshots,
    TimeSpan shiftStart,
    TimeSpan shiftEnd,
    string shiftName,
    DateTime enrollmentDate)
        {
            var results = new List<ShiftDrainResult>();

            // Group by local date (EST assumed) to evaluate each day's shift separately
            var snapshotsByDay = orderedSnapshots
                .GroupBy(s => s.Timestamp.Date)
                .OrderBy(g => g.Key);

            foreach (var group in snapshotsByDay)
            {
                var shiftStartTime = group.Key + shiftStart;
                var shiftEndTime = group.Key + shiftEnd;

                // Find the sample closest to shift start (with battery ≥ 95%)
                var startSnapshot = group
                    .Where(s => s.Timestamp >= shiftStartTime &&
                                s.Timestamp <= shiftEndTime &&
                                s.Battery >= 95)
                    .OrderBy(s => Math.Abs((s.Timestamp - shiftStartTime).TotalMinutes))
                    .FirstOrDefault();

                if (startSnapshot == null) continue;

                // Find the last sample before shift end
                var endSnapshot = group
                    .Where(s => s.Timestamp > startSnapshot.Timestamp &&
                                s.Timestamp <= shiftEndTime)
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefault();

                if (endSnapshot == null) continue;

                var shiftSnapshots = group
    .Where(s => s.Timestamp >= shiftStartTime && s.Timestamp <= shiftEndTime)
    .OrderBy(s => s.Timestamp)
    .ToList();

                if (!shiftSnapshots.Any()) continue;
                var first = shiftSnapshots.First();
                var last = shiftSnapshots.Last();

                double drain = startSnapshot.Battery - endSnapshot.Battery;
                double hours = (endSnapshot.Timestamp - startSnapshot.Timestamp).TotalHours;

                if (drain < 0 || hours <= 0) continue;

                results.Add(new ShiftDrainResult
                {
                    SerialNumber = serialNumber,
                    Restaurant = restaurant,
                    Model = model,
                    ShiftName = shiftName,
                    ShiftStart = startSnapshot.Timestamp.Date + shiftStart,
                    ShiftEnd = startSnapshot.Timestamp.Date + shiftEnd,
                    StartTimestamp = startSnapshot.Timestamp,
                    EndTimestamp = endSnapshot.Timestamp,
                    StartBattery = startSnapshot.Battery,
                    EndBattery = endSnapshot.Battery,
                    Drain = drain,
                    Hours = hours,
                    DropPerHour = drain / hours,
                    FirstSampleTime = first.Timestamp,
                    LastSampleTime = last.Timestamp,
                    EnrollmentDate = enrollmentDate
                });
            }

            return results;
        }

    }
}
