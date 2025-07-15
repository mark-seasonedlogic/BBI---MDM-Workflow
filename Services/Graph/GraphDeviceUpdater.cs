using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;
using Windows.UI.Popups;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using Microsoft.Graph.Models;
using NLog;
using NLog.LayoutRenderers;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Threading;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using System.Text.RegularExpressions;

public class GraphDeviceUpdater : IGraphDeviceUpdater
{
    private readonly GraphAuthHelper _graphAuthHelper;
    private readonly GraphServiceClient _graphClient;
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public class TokenProvider : IAccessTokenProvider
    {
        private readonly string _accessToken;

        public TokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
    }

    public GraphDeviceUpdater(GraphAuthHelper graphAuthHelper)
    {
        _graphAuthHelper = graphAuthHelper ?? throw new ArgumentNullException(nameof(graphAuthHelper));
        _graphClient = _graphAuthHelper.GetAuthenticatedClient();
    }

    public async Task UpdateIntuneDeviceCustomAttributesAsync(string managedDeviceId, Dictionary<string, string> customAttributes)
    {

        try
        {
            var updateData = new Dictionary<string, object>();

            foreach (var kvp in customAttributes)
            {
                updateData[$"comBBIHardwareSupport_{kvp.Key}"] = kvp.Value; // Prefix with Schema ID
            }

            await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId]
                .PatchAsync(new Microsoft.Graph.Models.ManagedDevice
                {
                    AdditionalData = updateData
                });

            Console.WriteLine($"✅ Successfully updated custom attributes for device {managedDeviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating custom attributes: {ex.Message}");
        }
    }
    public async Task GetIntuneDeviceCustomAttributesAsync(string managedDeviceId)
    {

        try
        {
            var device = await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] {
                    "id", "deviceName",
                    "comBBIHardwareSupport_BBI_AssetTag",
                    "comBBIHardwareSupport_BBI_Location",
                    "comBBIHardwareSupport_BBI_Owner"
                    };
                });

            Console.WriteLine($"✅ Device: {device.DeviceName}");
            Console.WriteLine($"🔹 Asset Tag: {device.AdditionalData?["comBBIHardwareSupport_BBI_AssetTag"] ?? "N/A"}");
            Console.WriteLine($"🔹 Location: {device.AdditionalData?["comBBIHardwareSupport_BBI_Location"] ?? "N/A"}");
            Console.WriteLine($"🔹 Owner: {device.AdditionalData?["comBBIHardwareSupport_BBI_Owner"] ?? "N/A"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error retrieving custom attributes: {ex.Message}");
        }
    }

    public async Task<string> CreateSchemaExtensionAsync()
    {
        var graphAuthHelper = new GraphAuthHelper();
        string token = await graphAuthHelper.GetDelegatedAccessTokenAsync();

        var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(token));
        var graphClient = new GraphServiceClient(new HttpClientRequestAdapter(authProvider));


        try
        {
            var schemaExtension = new Microsoft.Graph.Models.SchemaExtension
            {
                Id = "comBBIHardwareSupport", // Custom namespace
                Description = "Custom attributes for Intune devices",
                TargetTypes = new List<string> { "device" },
                Status = "InDevelopment", //This is required to ensure the schema is only available to this app for now
                Properties = new List<Microsoft.Graph.Models.ExtensionSchemaProperty>
            {
                new Microsoft.Graph.Models.ExtensionSchemaProperty { Name = "BBIAssetTag", Type = "String" },
                new Microsoft.Graph.Models.ExtensionSchemaProperty { Name = "BBILocation", Type = "String" },
                new Microsoft.Graph.Models.ExtensionSchemaProperty { Name = "BBIOwner", Type = "String" }
            }
            };

            var createdSchema = await graphClient.SchemaExtensions
                .PostAsync(schemaExtension);

            Console.WriteLine($"✅ Schema Extension Created: {createdSchema.Id}");
            return createdSchema.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating schema extension: {ex.Message}");
            return null;
        }
    }

    public async Task ProcessDevicesAsync()
    {
        var updater = new GraphDeviceUpdater(_graphAuthHelper);
        var schemaInfo = await updater.CreateSchemaExtensionAsync();
        var attributes = new Dictionary<string, string>
{
    { "BBI_AssetTag", "POS12345" },
    { "BBI_Location", "Tampa, FL" },
    { "BBI_Owner", "Restaurant IT" }
};

        // 🔹 Call the function with a valid Intune `ManagedDeviceId`
        await _graphAuthHelper.AddCustomSchemaAttributesToManagedDeviceAsync("06ae7517-6a15-48ed-a4ab-3c05f6b8a90f", attributes);
        try
        {
            //var devices = await _graphAuthHelper.GetDevicesByFilterAsync("serialNumber", "GFMQK9H4JJ");

            var managedDevices = await _graphAuthHelper.GetManagedDevicesBySerialAsync("GFMQK9H4JJ");
            var entraDevices = await _graphAuthHelper.GetManagedDevicesBySerialAsync("GFMQK9H4JJ");

            foreach (var device in managedDevices)
            {
                string serialNumber = device.SerialNumber ?? "Unknown";

                logger.Info($"Found Managed Device: {device.DeviceName}, Serial: {serialNumber}");
            }


            if (managedDevices == null || managedDevices.Count == 0)
            {
                await ShowMessageAsync("No devices found.");
                logger.Info("No managed devices found with serial number GFMQK9H4JJ");
                return;
            }

            foreach (var device in entraDevices)
            {
 
                // Extract [USER-GID] GUID
                string userGuid = device.UserId ?? "Unknown";
                if (string.IsNullOrEmpty(userGuid)) continue;

                //string userId = userGuid.Substring(11, 36);

                // Retrieve user information from Entra ID
                //var user = await _graphClient.Users[userId].GetAsync();

               // if (user != null)
                //{
                //    await ShowMessageAsync($"Device {device.DeviceName} owned by {user.DisplayName}");

                /*
                 * // Prepare attributes for update
                var attributes = new
                {
                    ExtensionAttributes = new
                    {
                        extensionAttribute1 = "1_9999"
                        }
                    };

                    // Convert to JSON and update the device in Entra ID
                    string jsonAttributes = JsonConvert.SerializeObject(attributes);

                    await _graphClient.Devices[device.Id].PatchAsync(new Device
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "ExtensionAttributes", attributes }
                        }
                    });

                }
                else
                {
                    await ShowMessageAsync($"Device {device.DeviceName} owned by unknown user {userId}");
                }*/
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Error: {ex.Message}");
        }
    }
    public async Task RenameDeviceBasedOnConventionAsync(
    string managedDeviceId,
    string concept,
    string restaurantNumber,
    string deviceFunction,
    string deviceNumber)
    {

        
        try
        {
            var device = await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId].GetAsync(q => {
                q.QueryParameters.Select = new[] { "id", "serialNumber", "operatingSystem", "model" };
            });
            //Ensure we are working qwith a device that is owned by BBI before continuing:
            if (string.IsNullOrWhiteSpace(device.DeviceName))
            {
                logger.Warn($"Device name is null or empty for device ID {managedDeviceId}. Skipping rename.");
                return;
            }

            var namePrefixPattern = @"^BBI-[A-Z]{3}\d{4}[A-Z]{3}\d{3}";

            if (!Regex.IsMatch(device.DeviceName, namePrefixPattern))
            {
                logger.Warn($"Device name '{device.DeviceName}' does not match expected format. Skipping rename.");
                return;
            }

            if (device == null)
            {
                logger.Warn($"No device found with ID {managedDeviceId}");
                return;
            }

            string deviceType = device.OperatingSystem?.ToLower() switch
            {
                "ios" => "iPad",
                "android" => "Android",
                _ => "Unknown"
            };

            string newName = $"{concept}{restaurantNumber}{deviceFunction}{deviceNumber} {deviceType} {device.OperatingSystem} {device.SerialNumber}";

            await _graphClient.DeviceManagement.ManagedDevices[managedDeviceId]
                .PatchAsync(new ManagedDevice
                {
                    DeviceName = newName
                });

            logger.Info($"✅ Renamed device {managedDeviceId} to '{newName}'");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"❌ Failed to rename device {managedDeviceId}");
            throw;
        }
    }

    private async Task ShowMessageAsync(string message)
    {
        var dialog = new MessageDialog(message);
        await dialog.ShowAsync();
    }
}
