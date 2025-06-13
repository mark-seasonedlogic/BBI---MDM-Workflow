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

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{ 
public partial class IntuneGroupsPageViewModel : ObservableObject
{
    private readonly IGraphADGroupService _groupService;

    public IntuneGroupsPageViewModel(IGraphADGroupService groupService)
    {
        _groupService = groupService;
    }
        public ObservableCollection<SimulationItem> SimulationItems { get; } = new()
{
    new SimulationItem
    {
        Title = "Simulate Enrollment",
        Description = "Pretend a device has enrolled in MDM.",
        ImagePath = "ms-appx:///Assets/Device_Enrolled.png",
        ExecuteCommand = new RelayCommand(() => Debug.WriteLine("Enrollment simulated"))
    },
    new SimulationItem
    {
        Title = "Simulate Rename Event",
        Description = "Trigger device renaming logic.",
        ImagePath = "ms-appx:///Assets/Device_Renamed.png",
        ExecuteCommand = new RelayCommand(() => Debug.WriteLine("Rename simulated"))
    }
};

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

                var metadata = await _groupService.GetGroupExtensionMetadataAsync(group.Id,extensionName);
                if (metadata == null || metadata.AdditionalData?.Count  == 0)
                {
                    await UiDialogHelper.ShowMessageAsync("No metadata found on this group.");
                    return;
                }

                var formatted = string.Join(Environment.NewLine, metadata.AdditionalData?.Select(kv => $"{kv.Key}: {kv.Value}"));
                await UiDialogHelper.ShowMessageAsync($"Metadata for {group.DisplayName}:\n\n{formatted}");
            }
            catch (Exception ex)
            {
                await UiDialogHelper.ShowMessageAsync($"Error: {ex.Message}");
            }
        }

    }
}