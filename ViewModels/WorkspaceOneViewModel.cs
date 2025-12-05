using BBIHardwareSupport.MDM.IntuneConfigManager;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Interfaces;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace BBIHardwareSupport.MDM.ViewModels
{
    public partial class WorkspaceOneViewModel : ObservableObject
    {
        private readonly IWorkspaceOneAuthService _authService;

        private readonly IWorkspaceOneDeviceService _deviceService;

        private readonly IWorkspaceOneSmartGroupsService _smartGroupsService;
        private readonly IProductsService _productService;
        private readonly IWorkspaceOneProfileService _profileService;

        private readonly ILogger<WorkspaceOneViewModel> _logger;
 

        public WorkspaceOneViewModel(IWorkspaceOneAuthService authService, IWorkspaceOneDeviceService deviceService, ILogger<WorkspaceOneViewModel> logger, IWorkspaceOneSmartGroupsService smartGroupsService, IProductsService productService, IWorkspaceOneProfileService profileService)
        {
            _authService = authService;
            _deviceService = deviceService;
            _smartGroupsService = smartGroupsService;
            _logger = logger;
            _productService = productService;
            _profileService = profileService;
        }
        private readonly IWorkspaceOneTaggingService _taggingService;

        public WorkspaceOneViewModel(
            IWorkspaceOneAuthService authService,
            IWorkspaceOneDeviceService deviceService,
            ILogger<WorkspaceOneViewModel> logger,
            IWorkspaceOneSmartGroupsService smartGroupsService,
            IProductsService productService,
            IWorkspaceOneProfileService profileService,
            IWorkspaceOneTaggingService taggingService)
        {
            _authService = authService;
            _deviceService = deviceService;
            _smartGroupsService = smartGroupsService;
            _logger = logger;
            _productService = productService;
            _profileService = profileService;
            _taggingService = taggingService;
        }

        public async Task<bool> SetCredentialsAsync(string Username, string Password, string ApiKey)
        {


            _authService.SetCredentials(Username, Password, ApiKey);
            IsAuthenticated = _authService.IsAuthenticated;

            return IsAuthenticated;
        }

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string hostUrl;

        [ObservableProperty]
        private bool isAuthenticated;

        [ObservableProperty]
        private string devicesJson;

        [ObservableProperty]
        private string apiKey;
        public ObservableCollection<UITileItem> UITileItems { get; } = new();
        public ObservableCollection<WorkspaceOneDevice> Devices { get; } = new();
        public ObservableCollection<WorkspaceOneSmartGroup> SmartGroups { get; } = new();
        public ObservableCollection<WorkspaceOneProduct> Products { get; } = new();
        public ObservableCollection<WorkspaceOneProfileSummary> Profiles { get; } = new();
        public ObservableCollection<WorkspaceOneProfileDetails> ProfileDetails { get; } = new();
        public ObservableCollection<TimeZoneTagReviewRow> TimeZoneTagReviewRows { get; } = new();

        private void InitializeUITileItems()
        {
            UITileItems.Clear();
            UITileItems.Add(new UITileItem
            {
                Title = "Load All BBI Devices",
                Description = "Load List with All Devices from BBI Org Id",
                ImagePath = "ms-appx:///Assets/Device_Enrolled.png",
                ExecuteCommand = new AsyncRelayCommand(async () => await LoadDevicesByOrgGroupAsync())
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All SmartGroups",
                Description = "Load List with All SmartGroups",
                ImagePath = "ms-appx:///Assets/Group_Metadata.png",
                ExecuteCommand = new RelayCommand(async () => await LoadAllSmartGroupsAsync())
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All Device Profiles",
                Description = "Load List with All Profiles",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new RelayCommand(async () => await LoadAllProfilesAsync())
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All Products",
                Description = "Load List with All Products",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new RelayCommand(async () => await LoadAllProductsAsync())
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All OEM Configuration Policies",
                Description = "Load List with All OEM Configuration Policies",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new RelayCommand(() => Debug.WriteLine("Load OEM Config Policies"))
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Review Time Zone Tags",
                Description = "Audit and bulk-tag POS devices per restaurant time zone",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new RelayCommand(async () => await ShowTimeZoneTagReviewAsync())
            });

        }
        public async Task OnLoadedAsync()
        {
            // Delay until after the constructor so RelayCommand can be initialized
            InitializeUITileItems();
            await Task.CompletedTask;

        }
        private void SortTimeZoneRows<TKey>(Func<TimeZoneTagReviewRow, TKey> keySelector, bool descending = false)
        {
            var ordered = descending
                ? TimeZoneTagReviewRows.OrderByDescending(keySelector).ToList()
                : TimeZoneTagReviewRows.OrderBy(keySelector).ToList();

            TimeZoneTagReviewRows.Clear();
            foreach (var r in ordered)
                TimeZoneTagReviewRows.Add(r);

            RefreshTimeZoneDisplayedItems();
        }
        // Using CommunityToolkit.Mvvm
        [ObservableProperty]
        private bool isTimeZoneReviewActive;

        [RelayCommand]
        private void SortTimeZoneByRestaurant()
            => SortTimeZoneRows(r => r.RestaurantCode);

        [RelayCommand]
        private void SortTimeZoneByTimeZone()
            => SortTimeZoneRows(r => r.TimeZone);

        [RelayCommand]
        private void SortTimeZoneByTagName()
            => SortTimeZoneRows(r => r.TagName);

        [RelayCommand]
        private void SortTimeZoneByTagId()
            => SortTimeZoneRows(r => r.TagId);

        [RelayCommand]
        private void SortTimeZoneByDeviceId()
            => SortTimeZoneRows(r => r.DeviceId);

        [RelayCommand]
        private void SortTimeZoneByEnrollmentUser()
            => SortTimeZoneRows(r => r.EnrollmentUserName);

        [RelayCommand]
        private void SelectAllTimeZoneRows()
        {
            foreach (var row in TimeZoneTagReviewRows)
                row.IsSelected = true;

            RefreshTimeZoneDisplayedItems();
        }

        [RelayCommand]
        private void DeselectAllTimeZoneRows()
        {
            foreach (var row in TimeZoneTagReviewRows)
                row.IsSelected = false;

            RefreshTimeZoneDisplayedItems();
        }

        [RelayCommand]
        private async Task AuthenticateAsync()
        {
            try
            {
                _authService.SetCredentials(Username, Password, ApiKey);

                // Test authentication — e.g., attempt to generate token or build headers
                var headers = await _authService.GetAuthorizationHeaderAsync(); // or GetAccessTokenAsync()

                IsAuthenticated = _authService.IsAuthenticated;
                DevicesJson = "Authentication succeeded.";
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                DevicesJson = $"Authentication failed: {ex.Message}";
            }
        }

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string deviceTestResult;

        [ObservableProperty]
        private ObservableCollection<object> displayedItems = new();

        private object _selectedArtifactType;

        public object SelectedArtifactType
        {
            get => _selectedArtifactType;
            set
            {
                SetProperty(ref _selectedArtifactType, value);
                OnPropertyChanged(nameof(IsDeviceView));
                OnPropertyChanged(nameof(IsSmartGroupView));
                OnPropertyChanged(nameof(IsProductView));
                OnPropertyChanged(nameof(IsProfileView));
                OnPropertyChanged(nameof(IsTimeZoneReviewView));
            }
        }
        public bool IsTimeZoneReviewView => SelectedArtifactType is TimeZoneTagReviewRow;
        public bool IsDeviceView => SelectedArtifactType is WorkspaceOneDevice;
        public bool IsSmartGroupView => SelectedArtifactType is WorkspaceOneSmartGroup;
        public bool IsProductView => SelectedArtifactType is WorkspaceOneProduct;
        public bool IsProfileView => SelectedArtifactType is WorkspaceOneProfileSummary;
        private void RefreshTimeZoneDisplayedItems()
        {
            DisplayedItems = new ObservableCollection<object>(TimeZoneTagReviewRows.Cast<object>());
            SelectedArtifactType = TimeZoneTagReviewRows.FirstOrDefault();
        }

        public async Task LoadAllProductsAsync()
        {
            try
            {
                Products.Clear();

                var products = await _productService.GetAllProductsAsync();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                // ✅ Replace the collection to trigger UI update
                DisplayedItems = new ObservableCollection<object>(products);

                // ✅ Update the artifact type so the template selector works
                SelectedArtifactType = products.FirstOrDefault();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load SmartGroups.");
                await UiDialogHelper.ShowMessageAsync("Error loading SmartGroups.");
            }
        }
        public async Task LoadAllProfilesAsync()
        {
            try
            {
                Profiles.Clear();
                ProfileDetails.Clear();

                var profiles = await _profileService.GetAllProfilesAsync();
                foreach (var profile in profiles)
                {
                    _logger.LogInformation("Loaded profile summary: {ProfileName} (ID: {ProfileId})", profile.ProfileName, profile.ProfileId);
                    Profiles.Add(profile);
                    _logger.LogInformation("Loading details for profile ID {ProfileId}", profile.ProfileId);
                    WorkspaceOneProfileDetails currProfile = await _profileService.GetProfileDetailsAsync(profile.ProfileId);
                    ProfileDetails.Add(currProfile);
                    //Write out to disk:

                    string json = System.Text.Json.JsonSerializer.Serialize(currProfile, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText($"C:\\Users\\MarkYoung\\source\\repos\\BBI - MDM Workflow\\Documentation\\WorkspaceOneArtifacts\\Device Profiles\\{profile.ProfileName}.json",json);
                }

                // ✅ Replace the collection to trigger UI update
                DisplayedItems = new ObservableCollection<object>(profiles.Cast<object>());

                // ✅ Update the artifact type so the template selector works
                SelectedArtifactType = profiles.FirstOrDefault();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Profiles.");
                await UiDialogHelper.ShowMessageAsync("Error loading Profiles.");
            }
        }

        public async Task LoadAllSmartGroupsAsync()
        {
            try
            {
                SmartGroups.Clear();

                var groups = await _smartGroupsService.GetAllSmartGroupsAsync();
                foreach (var group in groups)
                {
                    SmartGroups.Add(group);
                }

                // ✅ Replace the collection to trigger UI update
                DisplayedItems = new ObservableCollection<object>(groups.Cast<object>());

                // ✅ Update the artifact type so the template selector works
                SelectedArtifactType = groups.FirstOrDefault();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load SmartGroups.");
                await UiDialogHelper.ShowMessageAsync("Error loading SmartGroups.");
            }
        }
        public async Task LoadDevicesByOrgGroupAsync()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            DeviceTestResult = string.Empty;
            DevicesJson = string.Empty;
            Devices.Clear(); // important to prevent duplicates

            try
            {
                string orgGroupId = "570";
                var stopwatch = Stopwatch.StartNew();

                List<JObject> rawDevices = await _deviceService.GetAllAndroidDevicesByOrgExAsync(orgGroupId);
                stopwatch.Stop();

                Debug.WriteLine($"Retrieved {rawDevices.Count} Android devices in {stopwatch.ElapsedMilliseconds} ms.");
                DeviceTestResult = $"Retrieved {rawDevices.Count} Android devices.";

                foreach (var j in rawDevices)
                {
                    WorkspaceOneDevice device = j.ToObject<WorkspaceOneDevice>();
                    if (device != null)
                        Devices.Add(device);
                }

                DevicesJson = string.Join(Environment.NewLine,
                    Devices.Take(5).Select(d =>
                        $"- Serial: {d.SerialNumber}, Name: {d.DeviceFriendlyName}"));

                DisplayedItems = new ObservableCollection<object>(Devices.Cast<object>());
                SelectedArtifactType = Devices.FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                DeviceTestResult = "HTTP error: " + ex.Message;
                Debug.WriteLine($"[HTTP Error] {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                DeviceTestResult = "Authentication error: " + ex.Message;
                Debug.WriteLine($"[Auth Error] {ex}");
            }
            catch (Exception ex)
            {
                DeviceTestResult = "Unexpected error: " + ex.Message;
                Debug.WriteLine($"[Unhandled Error] {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        public async Task ShowTimeZoneTagReviewAsync()
        {
            try
            {
                // 1) Prompt for input files (uses UiDialogHelper, like your other dialogs). :contentReference[oaicite:7]{index=7}
                var masterPath = await UiDialogHelper.PromptForFileAsync(App.MainWindow.Content.XamlRoot, ".csv","Choose Master Restaurant Directory CSV");
                if (string.IsNullOrWhiteSpace(masterPath)) return;

                var restaurantsPath = await UiDialogHelper.PromptForFileAsync(App.MainWindow.Content.XamlRoot, ".csv", "Choose List of Restaurants CSV");
                if (string.IsNullOrWhiteSpace(restaurantsPath)) return;

                // Ensure devices are loaded (or call device service directly)
                if (Devices.Count == 0)
                    await LoadDevicesByOrgGroupAsync();

                var request = new TimeZoneTagAuditRequest
                {
                    MasterCsvPath = masterPath,
                    RestaurantsPath = restaurantsPath,
                    Devices = Devices.ToList(),
                    OgId = 570 // or bind from settings
                };
                // Set the TimeZoneReviewMode to true:
                IsTimeZoneReviewActive = true;
                // 2) Run the audit
                var plan = await _taggingService.InvokeTimeZoneTagAuditAsync(request);
                if (plan == null || plan.Count == 0)
                {
                    await UiDialogHelper.ShowMessageAsync("No plan items were produced.");
                    return;
                }

                // 3) Flatten to per-device rows
                var rows = new List<TimeZoneTagReviewRow>();
                foreach (var p in plan)
                {
                    if (p.TagId is null || p.DeviceIds == null || p.DeviceIds.Count == 0)
                        continue;

                    foreach (var dev in p.DeviceIds)
                    {
                        // Find corresponding device to get enrollment user name
                        var device = Devices.FirstOrDefault(d =>
                            string.Equals(d.DeviceId.ToString(), dev, StringComparison.OrdinalIgnoreCase));

                        rows.Add(new TimeZoneTagReviewRow
                        {
                            RestaurantCode = p.RestaurantCode,
                            TimeZone = p.TimeZone,
                            TagName = p.TagName,
                            TagId = p.TagId.Value,
                            DeviceId = dev,
                            EnrollmentUserName = device?.UserName // or EnrollmentUserName property if you have one
                        });
                    }
                }

                if (!rows.Any())
                {
                    await UiDialogHelper.ShowMessageAsync("Nothing to review (no devices needing tags or no TagId resolved).");
                    return;
                }

                // 4) Pull membership per TagId
                var tagIds = rows.Select(r => r.TagId).Distinct().ToList();
                var memberMap = new Dictionary<int, HashSet<string>>();
                foreach (var tid in tagIds)
                {
                    memberMap[tid] = await _taggingService.GetDevicesForTagAsync(tid, pageSize: 500);
                }

                TimeZoneTagReviewRows.Clear();
                foreach (var row in rows)
                {
                    var set = memberMap[row.TagId];
                    row.AlreadyTagged = set != null && set.Contains(row.DeviceId);
                    row.IsSelected = !row.AlreadyTagged; // default: only ones that need tagging
                    TimeZoneTagReviewRows.Add(row);
                }

                RefreshTimeZoneDisplayedItems();
                IsTimeZoneReviewActive = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running WS1 Time Zone Tag Review.");
                await UiDialogHelper.ShowMessageAsync($"Error running time zone tag review: {ex.Message}");
            }
        }


        [RelayCommand]
        private async Task LoadDevicesAsync()
        {
            

            try
            {
                using var httpClient = new HttpClient();
                var headers = await _authService.GetAuthorizationHeaderAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_authService.GetBaseUri(), "/API/mdm/devices/search"));
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                DevicesJson = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                DevicesJson = $"Error: {ex.Message}";
            }
        }
    }
}
