using BBIHardwareSupport.MDM.IntuneConfigManager;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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

namespace BBIHardwareSupport.MDM.ViewModels
{
    public partial class WorkspaceOneViewModel : ObservableObject
    {
        private readonly IWorkspaceOneAuthService _authService;

        private readonly IWorkspaceOneDeviceService _deviceService;

        private readonly IWorkspaceOneSmartGroupsService _smartGroupsService;
        private readonly IProductsService _productService;
        private readonly IWorkspaceOneProfileService _profileService;
        private readonly IWorkspaceOneProfileExportService _profileExportService;

        private readonly ILogger<WorkspaceOneViewModel> _logger;
        private readonly IWorkspaceOneAdminsService _adminsService;

       private readonly IWorkspaceOneTaggingService _taggingService;

        public WorkspaceOneViewModel(
            IWorkspaceOneAuthService authService,
            IWorkspaceOneDeviceService deviceService,
            ILogger<WorkspaceOneViewModel> logger,
            IWorkspaceOneSmartGroupsService smartGroupsService,
            IProductsService productService,
            IWorkspaceOneProfileService profileService,
            IWorkspaceOneProfileExportService profileExportService,
            IWorkspaceOneTaggingService taggingService,
            IWorkspaceOneAdminsService adminsService, 
            MainViewModel mainVM)
        {
            _authService = authService;
            _deviceService = deviceService;
            _smartGroupsService = smartGroupsService;
            _logger = logger;
            _productService = productService;
            _profileService = profileService;
            _profileExportService = profileExportService;
            _taggingService = taggingService;
            _adminsService = adminsService;
            _mainVM = mainVM;
        }

        public async Task<bool> SetCredentialsAsync(string Username, string Password, string ApiKey)
        {


            _authService.SetCredentials(Username, Password, ApiKey);
            IsAuthenticated = _authService.IsAuthenticated;

            return IsAuthenticated;
        }
        public Task<bool> SetCredentialsAsync(WorkspaceOneCredentials creds)
        {
            if (creds is null) throw new ArgumentNullException(nameof(creds));

            _authService.SetCredentials(creds.Username, creds.Password, creds.Environment);
            IsAuthenticated = _authService.IsAuthenticated;

            return Task.FromResult(IsAuthenticated);
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
        public Func<Task<bool>>? LoginRequested { get; set; }
        private readonly MainViewModel _mainVM;
        private Task<bool> RequestLoginAsync()
            => LoginRequested?.Invoke() ?? Task.FromResult(false);

        private void InitializeUITileItems()
        {
            UITileItems.Clear();
            UITileItems.Add(new UITileItem
            {
                Title = "Sign in to Workspace ONE",
                Description = "Enter credentials to enable Workspace ONE actions",
                ImagePath = "ms-appx:///Assets/Login.png", // or reuse an existing icon
                ExecuteCommand = new AsyncRelayCommand(async () => await RequestLoginAsync())
            });
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
                ExecuteCommand = new AsyncRelayCommand(LoadAllSmartGroupsAsync)
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All Device Profiles",
                Description = "Load List with All Profiles",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new AsyncRelayCommand(LoadAllProfilesAsync)
            });
            UITileItems.Add(new UITileItem
            {
                Title = "Load All Products",
                Description = "Load List with All Products",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new AsyncRelayCommand(LoadAllProductsAsync)
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
                ExecuteCommand = new AsyncRelayCommand(ShowTimeZoneTagReviewAsync)
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
            if (_mainVM.IsLoading) return;
            if (!IsAuthenticated)
            {
                DeviceTestResult = "Please sign in to Workspace ONE first.";
                return;
            }
            BeginLoad("Loading Products...");
            Products.Clear(); // important to prevent duplicates
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
            finally
            { 
                IsLoading = false;
                EndLoad();
            }
        }
        public async Task LoadAllProfilesAsync()
        {
            if (_mainVM.IsLoading) return;
            if (!IsAuthenticated)
            {
                DeviceTestResult = "Please sign in to Workspace ONE first.";
                return;
            }
            BeginLoad("Loading Profiles...");
            Profiles.Clear(); // important to prevent duplicates
            try
            {
                Profiles.Clear();
                ProfileDetails.Clear(); // can delete later if you stop using it

                var profiles = await _profileService.GetAllProfilesAsync();

                foreach (var profile in profiles)
                    Profiles.Add(profile);

                // ✅ Trigger UI update
                DisplayedItems = new ObservableCollection<object>(profiles.Cast<object>());
                SelectedArtifactType = profiles.FirstOrDefault();

                // ✅ Preload export cache (record + payload-details), throttled inside service
                _profileExportService.ClearCache(); // optional; keep if you want always-fresh
                await _profileExportService.PreloadAsync(profiles);

                // ✅ Optional: persist cache for dev
                // await _profileExportService.SaveCacheToDiskAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Profiles.");
                await UiDialogHelper.ShowMessageAsync("Error loading Profiles.");
            }
            finally
            {
                EndLoad();
            }
        }

        public async Task LoadAllSmartGroupsAsync()
        {
            if (_mainVM.IsLoading) return;
            if (!IsAuthenticated)
            {
                DeviceTestResult = "Please sign in to Workspace ONE first.";
                return;
            }
            BeginLoad("Loading SmartGroups...");
            SmartGroups.Clear(); // important to prevent duplicates
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
            finally
            {
                IsLoading = false;
                EndLoad();
            }

        }
        public async Task<bool> ValidateLoginAsync(string username)
        {
            try
            {
                IsLoading = true;
                DeviceTestResult = "Validating Workspace ONE credentials...";

                var admin = await _adminsService.GetAdminByUsernameAsync(username);

                if (admin == null)
                {
                    DeviceTestResult = "Login validated, but admin user was not found (or not authorized).";
                    IsAuthenticated = false;
                    return false;
                }

             /*Add Later   // Success: store/display identity if you want
                CurrentAdminUserName = admin.UserName; // optional property for UI
                DeviceTestResult = $"Signed in as {admin.FirstName} {admin.LastName} ({admin.LocationGroup})";
                IsAuthenticated = true;

                // IMPORTANT: if you gate tile commands by IsAuthenticated, notify CanExecute here
                NotifyAllCommandsCanExecuteChanged();
             */
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                DeviceTestResult = "Invalid username/password/API key (401).";
                IsAuthenticated = false;
                return false;
            }
            catch (Exception ex)
            {
                DeviceTestResult = $"Login validation failed: {ex.Message}";
                IsAuthenticated = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadDevicesByOrgGroupAsync()
        {
            if (_mainVM.IsLoading) return;

            if (!IsAuthenticated)
            {
                DeviceTestResult = "Please sign in to Workspace ONE first.";
                _mainVM.StatusMessage = "Not authenticated to Workspace ONE.";
                return;
            }

            BeginLoad("Loading Devices...");
            SetGlobalStatus("Loading devices from Workspace ONE...");
            Devices.Clear();

            try
            {
                string orgGroupId = "570";
                var stopwatch = Stopwatch.StartNew();

                List<JObject> rawDevices = await _deviceService.GetAllAndroidDevicesByOrgExAsync(
                    orgGroupId,
                    progress =>
                    {
                        // push progress to UI thread
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (progress.TotalItems.HasValue && progress.TotalItems.Value > 0)
                            {
                                var pct = (progress.ItemsLoaded * 100.0) / progress.TotalItems.Value;
                                DeviceTestResult =
                                    $"Loading… {progress.ItemsLoaded}/{progress.TotalItems} ({pct:0}%)";
                                _mainVM.StatusMessage =
                                    $"Loading… {progress.ItemsLoaded}/{progress.TotalItems} ({pct:0}%)";
                            }
                            else
                            {
                                DeviceTestResult =
                                    $"Loading… page {progress.CurrentPage + 1}, loaded {progress.ItemsLoaded}";
                                _mainVM.StatusMessage =
                                    $"Loading… page {progress.CurrentPage + 1}, loaded {progress.ItemsLoaded}";
                            }
                        });
                    });

                stopwatch.Stop();

                Debug.WriteLine($"Retrieved {rawDevices.Count} Android devices in {stopwatch.ElapsedMilliseconds} ms.");
                DeviceTestResult = $"Retrieved {rawDevices.Count} Android devices in {stopwatch.ElapsedMilliseconds} ms.";

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
                EndLoad();
            }
        }
        public async Task ShowTimeZoneTagReviewAsync()
        {
            if (_mainVM.IsLoading) return;
            if (!IsAuthenticated)
            {
                DeviceTestResult = "Please sign in to Workspace ONE first.";
                _mainVM.StatusMessage = "Not authenticated to Workspace ONE.";
                return;
            }

            BeginLoad("Loading Time Zone Tag Review…");
            TimeZoneTagReviewRows.Clear(); // important to prevent duplicates
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
            finally
            {
                IsLoading = false;
                EndLoad();
            }

        }

        private void BeginLoad(string status = "")
        {
            IsLoading = true;
            SetGlobalLoading(true, status);
            DeviceTestResult = status;
            DevicesJson = string.Empty;
        }

        private void SetGlobalLoading(bool isLoading, string? message = null)
        {
            _mainVM.IsLoading = isLoading;

            if (message != null)
                _mainVM.StatusMessage = message;
        }

        private void SetGlobalStatus(string message)
        {
            _mainVM.StatusMessage = message;
        }

        private void EndLoad()
        {
            IsLoading = false;
            _mainVM.IsLoading = false;
            
        }

    }


}
