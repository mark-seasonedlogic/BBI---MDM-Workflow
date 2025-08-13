using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Linq;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.Services.WorkspaceOne;
using Microsoft.Extensions.Logging;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using Newtonsoft.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using System.Text.Json;
using System.IO;

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
        }
        public async Task OnLoadedAsync()
        {
            // Delay until after the constructor so RelayCommand can be initialized
            InitializeUITileItems();
            await Task.CompletedTask;

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
            }
        }

        public bool IsDeviceView => SelectedArtifactType is WorkspaceOneDevice;
        public bool IsSmartGroupView => SelectedArtifactType is WorkspaceOneSmartGroup;
        public bool IsProductView => SelectedArtifactType is WorkspaceOneProduct;
        public bool IsProfileView => SelectedArtifactType is WorkspaceOneProfileSummary;

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
