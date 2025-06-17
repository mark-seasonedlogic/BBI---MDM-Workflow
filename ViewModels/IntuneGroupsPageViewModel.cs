using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Windows.Services.Maps;
using static BBIHardwareSupport.MDM.IntuneConfigManager.Models.BBIEntraGroupExtension;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public partial class IntuneGroupsPageViewModel : ObservableObject
    {
        private readonly IGraphADGroupService _groupService;
        private readonly IGraphIntuneManagedAppService _appService;
        private readonly IGraphIntuneConfigurationService _configService;
        private readonly IGraphDeviceCategoryService _deviceCategoryService;
        private readonly ILogger<IntuneGroupsPageViewModel> _logger;

        public IntuneGroupsPageViewModel(IGraphADGroupService groupService, IGraphIntuneManagedAppService appService, IGraphIntuneConfigurationService configService,IGraphDeviceCategoryService deviceCategoryService, ILogger<IntuneGroupsPageViewModel> logger)
        {
            _groupService = groupService;
            _appService = appService;
            _configService = configService;
            _deviceCategoryService = deviceCategoryService;
            _logger = logger;



        }
        public ObservableCollection<SimulationItem> SimulationItems { get; } = new();
        private void InitializeSimulationItems()
        {
            SimulationItems.Add(new SimulationItem
            {
                Title = "Simulate Enrollment",
                Description = "Pretend a device has enrolled in MDM.",
                ImagePath = "ms-appx:///Assets/Device_Enrolled.png",
                ExecuteCommand = new AsyncRelayCommand(SimulateEnrollmentAsync)
            });
            SimulationItems.Add(new SimulationItem
            {
                Title = "Inspect Group Custom Metadata",
                Description = "Display Custom Metadata for an Intune Group.",
                ImagePath = "ms-appx:///Assets/Group_Metadata.png",
                ExecuteCommand = new RelayCommand(() => Debug.WriteLine("Group Metadata"))
            });
            SimulationItems.Add(new SimulationItem
            {
                Title = "Simulate Rename Event",
                Description = "Trigger device renaming logic.",
                ImagePath = "ms-appx:///Assets/Device_Renamed.png",
                ExecuteCommand = new RelayCommand(() => Debug.WriteLine("Rename simulated"))
            });
        }
        public async Task OnLoadedAsync()
        {
            // Delay until after the constructor so RelayCommand can be initialized
            InitializeSimulationItems();
            await Task.CompletedTask;
        }

        [RelayCommand]
        public async Task SimulateEnrollmentAsync()
        {
            var input = await UiDialogHelper.PromptForTextAsync("Enter Device Name (e.g., OBS1099CIM)");
            if (string.IsNullOrWhiteSpace(input))
                return;

            // Call your existing workflow logic here
            await SimulateEnrollmentWorkflowAsync(input);
        }

        [RelayCommand]
        public async Task CreateDynamicGroupAsync()
        {
            try
            {
                string displayName = "Demo Dynamic Group";
                string description = "Auto-created dynamic group from BBI MDM Workflow";
                string rule = "(device.deviceOSType -eq \"Android\")"; // example
                var owners = new List<string> { "owner-object-id-1" };

                string groupId = await _groupService.CreateDynamicGroupAsync(displayName, description, rule, owners);

                // Optional: Show a dialog or toast
                Debug.WriteLine($"Group created: {groupId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create group: {ex.Message}");
                // Optional: Show error message to the user
            }
        }



        [RelayCommand]
        public async Task InspectGroupMetadataAsync()
        {
            try
            {
                // Prompt user for group name or ID (OBS1234 style)
                var groupDisplayName = await UiDialogHelper.PromptForTextAsync("Enter Group Code (e.g., OBS1234):");
                if (string.IsNullOrWhiteSpace(groupDisplayName)) return;
                string extensionName = "com.bbi.entra.group.metadata"; //Update to pull this in dynamically
                var group = await _groupService.FindGroupByDisplayNameAsync(groupDisplayName);
                if (group == null)
                {
                    await UiDialogHelper.ShowMessageAsync($"Group '{groupDisplayName}' not found.");
                    return;
                }

                var metadata = await _groupService.GetGroupExtensionMetadataAsync(group.Id, extensionName);
                if (metadata == null || metadata.AdditionalData?.Count == 0)
                {
                    await UiDialogHelper.ShowMessageAsync("No metadata found on this group.",$"OpenExtension Metadata For {group.DisplayName}");
                    return;
                }

                var formatted = string.Join(Environment.NewLine, metadata.AdditionalData?.Select(kv => $"{kv.Key}: {kv.Value}"));
                await UiDialogHelper.ShowMessageAsync($"{formatted}", $"OpenExtension Metadata for { group.DisplayName}");
            }
            catch (Exception ex)
            {
                await UiDialogHelper.ShowMessageAsync($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles a simulated enrollment scenario for a device.
        /// </summary>
        private async Task SimulateEnrollmentWorkflowAsync(string deviceName)
        {
            try
            {
                _logger.LogInformation("Starting simulated enrollment workflow for device: {DeviceName}", deviceName);

                // 1. Parse device name
                if (deviceName.Length < 8)
                {
                    _logger.LogWarning("Device name too short to parse required components: {DeviceName}", deviceName);
                    await UiDialogHelper.ShowMessageAsync("Invalid device name format.");
                    return;
                }

                string brand = deviceName[..3];
                string number = deviceName.Substring(3, 4);
                string function = deviceName[7..];

                if (!Enum.TryParse<ConceptPrefix>(brand, out var conceptPrefix))
                {
                    _logger.LogError("Unrecognized brand abbreviation: {Brand}", brand);
                    await UiDialogHelper.ShowMessageAsync("Unrecognized brand code.");
                    return;
                }

                int cdId = (int)conceptPrefix;
                string groupRule = string.Empty;

                // 2. Ensure device category (only needed for CIM)
                if (function.Equals("CIM", StringComparison.OrdinalIgnoreCase))
                {
                    string categoryName = $"BBI - iOS {function}";
                    var category = await _deviceCategoryService.GetDeviceCategoryByNameAsync(categoryName);
                    if (category == null)
                    {
                        category = await _deviceCategoryService.CreateDeviceCategoryAsync(categoryName, $"iOS devices for {function} function");
                        if (category == null)
                        {
                            _logger.LogError("Failed to create device category: {CategoryName}", categoryName);
                            await UiDialogHelper.ShowMessageAsync($"❌ Unable to create device category {categoryName}.");
                            return;
                        }
                    }

                    groupRule = $"(device.displayName -startsWith \"{brand}{number}\") and (device.deviceCategory -eq \"{categoryName}\")";
                }

                // 3. Ensure group exists and metadata is correct
                string groupDisplay = $"{brand} {number} - {function}";
                var group = await _groupService.FindGroupByDisplayNameAsync(groupDisplay);
                string groupId = group?.Id ?? string.Empty;

                if (group == null)
                {
                    List<string> owners = new() { "2c1c9531-75bd-4cf1-897d-e1869dc5deec" }; // Mark Young ADM
                    groupId = await _groupService.CreateDynamicGroupAsync(groupDisplay, groupDisplay, groupRule, owners);
                    if (string.IsNullOrEmpty(groupId))
                    {
                        _logger.LogError("Failed to create dynamic group for {Display}", groupDisplay);
                        await UiDialogHelper.ShowMessageAsync($"❌ Failed to create the group {groupDisplay}");
                        return;
                    }
                }

                Dictionary<string, object> metadata = new()
        {
            { "BrandAbbreviation", brand },
            { "RestaurantNumber", number },
            { "RestaurantCdId", $"{cdId}_{number}" }
        };

                var currentMetadata = await _groupService.GetOpenExtensionAsync(groupId, "com.bbi.entra.group.metadata");
                bool needsUpdate = currentMetadata == null || !metadata.All(kv => currentMetadata.TryGetValue(kv.Key, out var v) && v?.ToString() == kv.Value?.ToString());

                if (needsUpdate)
                {
                    await _groupService.AddOrUpdateGroupOpenExtensionAsync(groupId, metadata);
                    _logger.LogInformation("Group metadata updated for group {GroupId}", groupId);
                }
                else
                {
                    _logger.LogInformation("Group metadata already up-to-date for group {GroupId}", groupId);
                }

                // 4. Assign app if device is CIM
                if (!function.Equals("CIM", StringComparison.OrdinalIgnoreCase)) return;

                var app = await _appService.GetManagedAppByNameAsync("Olo Expo");
                if (app == null)
                {
                    _logger.LogError("Olo Expo app not found.");
                    await UiDialogHelper.ShowMessageAsync("❌ Olo Expo iOS app not found.");
                    return;
                }
                _logger.LogInformation("Found Olo Expo app: {AppName} ({AppId}).  Assigning {AppName} to group {GroupId}", app["displayName"], app["id"], app["displayName"], groupId);

                var assignment = await _appService.AssignAppToGroupAsync(app["id"]?.ToString(), groupId);
                
                string appId = app["id"]?.ToString();
                string storeIdentifierValue = $"{cdId}_{number}";
                var configs = await _configService.FindManagedAppConfigurationsByTargetedAppAsync(appId);

                var match = configs.FirstOrDefault(cfg => cfg["settings"] is JArray settings &&
                    settings.Any(s =>
                        s?["appConfigKey"]?.ToString() == "storeIdentifierValues" &&
                        s?["appConfigKeyValue"]?.ToString() == storeIdentifierValue));

                if (match != null)
                {
                    _logger.LogInformation("Matching app config already exists: {ConfigName}", match["displayName"]);
                    await UiDialogHelper.ShowMessageAsync($"✅ Matching config already exists: {match["displayName"]}");
                    return;
                }

                string masterId = brand switch
                {
                    "BFG" => "2e635840-1408-4b00-a66a-5194229d8c28",
                    "CIG" => "645e588a-425c-448d-be84-43bb19fa882f",
                    "OBS" => "8e8d9201-fe77-42db-aa03-3c889ca27a1a",
                    _ => throw new InvalidOperationException("Unknown brand abbreviation.")
                };
                _logger.LogDebug("Retrieving master app configuration for brand {Brand} with ID {MasterId}", brand, masterId);
                var master = await _configService.GetManagedAppConfigurationByIdAsync(masterId);
                if (master == null)
                {
                    _logger.LogError("Failed to retrieve master app configuration for brand {Brand}", brand);
                    await UiDialogHelper.ShowMessageAsync("❌ Failed to retrieve master configuration.");
                    return;
                }
                _logger.LogDebug("Master configuration retrieved: {ConfigName}", master["displayName"]);
                _logger.LogInformation("Cloning master configuration for brand {Brand} with number {Number}", brand, number);

                var cloned = await _configService.CloneManagedAppConfigurationAsync(master, $"BBI - OLO Expo - {brand}{number}", metadata);
                if (cloned == null)
                {
                    _logger.LogError("Failed to clone configuration from master for {Brand} {Number}", brand, number);
                    await UiDialogHelper.ShowMessageAsync("❌ Failed to clone configuration.");
                    return;
                }

                _logger.LogInformation("Successfully cloned and applied new configuration: {ConfigName}", cloned["displayName"]);
                await UiDialogHelper.ShowMessageAsync($"✅ New config cloned and updated: {cloned["displayName"]}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during simulated enrollment workflow.");
                await UiDialogHelper.ShowMessageAsync("❌ An unexpected error occurred during the workflow.");
            }
        }


    }
}