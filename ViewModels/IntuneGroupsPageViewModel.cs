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

        public IntuneGroupsPageViewModel(IGraphADGroupService groupService, IGraphIntuneManagedAppService appService, IGraphIntuneConfigurationService configService,IGraphDeviceCategoryService deviceCategoryService)
        {
            _groupService = groupService;
            _appService = appService;
            _configService = configService;
            _deviceCategoryService = deviceCategoryService;



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

        private async Task SimulateEnrollmentWorkflowAsync(string deviceName)
        {
            string brand = deviceName[..3];
            string number = deviceName.Substring(3, 4);
            string function = deviceName[7..];

            int cdId = 0;
            if (Enum.TryParse<ConceptPrefix>(brand, out var result))
            {
                cdId = (int)result;
            }
            else
            {
                return;
            }

            // Ensure device category exists (only for CIM devices)
            if (function.ToUpperInvariant() == "CIM")
            {
                var deviceCategory = await _deviceCategoryService.GetDeviceCategoryByNameAsync($"BBI - iOS {function}");
                if (deviceCategory == null)
                {
                    deviceCategory = await _deviceCategoryService.CreateDeviceCategoryAsync($"BBI - iOS {function}", $"iOS devices for {function} function");
                    if (deviceCategory == null)
                    {
                        await UiDialogHelper.ShowMessageAsync($"❌ Unable to create device category BBI - iOS {function}.");
                        return;
                    }
                }
            }

            string groupName = $"{brand}{number}";
            string groupDisplay = $"{brand} {number} - {function}";
            string groupRule = $"(device.displayName -startsWith \"{brand}{number}\") and (device.deviceCategory -eq \"BBI - iOS {function}\")";
            string groupId = string.Empty;

            // 1. Ensure group exists (or create it)
            var group = await _groupService.FindGroupByDisplayNameAsync(groupDisplay);
            if (group == null)
            {
                List<string> owners = new List<string>
        {
            "2c1c9531-75bd-4cf1-897d-e1869dc5deec"
        };
                groupId = await _groupService.CreateDynamicGroupAsync(groupDisplay, groupDisplay, groupRule, owners);
                if (String.IsNullOrEmpty(groupId))
                {
                    await UiDialogHelper.ShowMessageAsync($"❌ Failed to create the group {groupDisplay}");
                    return;
                }
            }

            // 2. Assign OpenExtension metadata
            Dictionary<string, object> metadata = new Dictionary<string, object>
    {
        { "BrandAbbreviation", brand },
        { "RestaurantNumber", number },
        { "RestaurantCdId", String.Format("{0}_{1}", cdId, number) }
    };
            await _groupService.AddOrUpdateGroupOpenExtensionAsync(groupId, new Dictionary<string, object>
    {
        { "BrandAbbreviation", brand },
        { "RestaurantNumber", number },
        { "RestaurantCdId", String.Format("{0}_{1}", cdId, number) }
    });

            // 3. If CIM device, handle app config
            if (function.ToUpperInvariant() == "CIM")
            {
                var app = await _appService.GetManagedAppByNameAsync("Olo Expo");
                if (app == null)
                {
                    await UiDialogHelper.ShowMessageAsync("❌ Olo Expo iOS app not found.");
                    return;
                }

                string storeIdentifierValue = $"{(int)Enum.Parse(typeof(ConceptPrefix), brand)}_{number}";

                var configs = await _configService.FindManagedAppConfigurationsByTargetedAppAsync(app["id"]?.ToString());

                // Search for an existing config with correct storeIdentifierValue
                var matchingConfig = configs.FirstOrDefault(cfg =>
                {
                    var settingsArray = cfg["settings"] as JArray;
                    return settingsArray?.Any(setting =>
                        setting?["appConfigKey"]?.ToString() == "storeIdentifierValues" &&
                        setting?["appConfigKeyValue"]?.ToString() == storeIdentifierValue) == true;
                });

                if (matchingConfig != null)
                {
                    await UiDialogHelper.ShowMessageAsync($"✅ Matching config already exists: {matchingConfig["displayName"]}");
                    return;
                }

                // Clone the appropriate master config based on brand
                string masterConfigId = brand switch
                {
                    "BFG" => "2e635840-1408-4b00-a66a-5194229d8c28",
                    "CIG" => "645e588a-425c-448d-be84-43bb19fa882f",
                    "OBS" => "8e8d9201-fe77-42db-aa03-3c889ca27a1a",
                    _ => throw new InvalidOperationException("Unknown brand.")
                };

                var masterConfig = await _configService.GetManagedAppConfigurationByIdAsync(masterConfigId);
                if (masterConfig == null)
                {
                    await UiDialogHelper.ShowMessageAsync("❌ Failed to retrieve master configuration.");
                    return;
                }

                var clonedConfig = await _configService.CloneManagedAppConfigurationAsync(masterConfig, $"BBI - OLO Expo - {brand}{number}", metadata);
                if (clonedConfig == null)
                {
                    await UiDialogHelper.ShowMessageAsync("❌ Failed to clone configuration.");
                    return;
                }

                

                await UiDialogHelper.ShowMessageAsync($"✅ New config cloned and updated: {clonedConfig["displayName"]}");
            }
        }


    }
}