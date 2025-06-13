using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public class SchemaAdminViewModel
    {
        private readonly ISchemaExtensionRegistrar _registrar;
        private readonly IGraphADGroupService _groupService;

        public IRelayCommand RegisterSchemaCommand { get; }
        public IRelayCommand SaveOpenExtensionCommand { get; }

        /*
        public SchemaAdminViewModel(ISchemaExtensionRegistrar registrar)
        {
            _registrar = registrar;
            RegisterSchemaCommand = new RelayCommand(async () => await RegisterAsync());
        }
        */
        public SchemaAdminViewModel(IGraphADGroupService groupService)
        {
            _groupService = groupService;

            SaveOpenExtensionCommand = new RelayCommand(async () => await SaveOpenExtensionAsync());
        }
        private async Task SaveOpenExtensionAsync()
        {
            try
            {
                string groupId = "fdf4c308-ec2e-451e-8b9e-6ab600ab2da2"; // Using ID for OBS9921 for testing - Replace with dynamic selection if needed
                var extensionData = new Dictionary<string, object>
        {
            { "BrandAbbreviation", "OBS" },
            { "RestaurantNumber", "9921" },
            { "RestaurantName", "Outback Steakhouse" }
        };

                await _groupService.AddOrUpdateOpenExtensionAsync(groupId, "com.bbi.entra.group.metadata", extensionData);
                StatusMessage = "Open Extension saved successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
        private async Task RegisterAsync()
        {
            try
            {
                await _registrar.RegisterBbiEntraGroupExtensionAsync();
                StatusMessage = "Schema registered successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public string StatusMessage { get; private set; }
        public async Task LoadGroupMetadataAsync(string groupId)
        {
            var extension = await _groupService.GetGroupExtensionMetadataAsync(groupId, "com.bbi.entra.group.metadata");

            if (extension?.AdditionalData != null)
            {
                RestaurantCdId = extension.AdditionalData["RestaurantCdId"]?.ToString();
                BrandAbbreviation = extension.AdditionalData["BrandAbbreviation"]?.ToString();
                RestaurantNumber = extension.AdditionalData["RestaurantNumber"]?.ToString();
                RestaurantName = extension.AdditionalData["RestaurantName"]?.ToString();
            }
            else
            {
                // Handle case where extension doesn't exist
                StatusMessage = "No metadata found for this group.";
            }
        }

        // You may want to expose these properties as public string properties with INotifyPropertyChanged for UI binding:
        public string RestaurantCdId { get; set; }
        public string BrandAbbreviation { get; set; }
        public string RestaurantNumber { get; set; }
        public string RestaurantName { get; set; }

    }

}
