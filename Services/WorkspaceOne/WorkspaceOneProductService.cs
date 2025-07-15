using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport.MDM.Services.WorkspaceOne
{
    /// <summary>
    /// Implements IProductsService to retrieve product provisioning data from Workspace ONE.
    /// </summary>
    public class WorkspaceOneProductsService : WorkspaceOneServiceBase, IProductsService
    {

        private readonly ILogger<WorkspaceOneProductsService> _logger;

        public WorkspaceOneProductsService(HttpClient httpClient, IWorkspaceOneAuthService authService, ILogger<WorkspaceOneProductsService> logger) : base(httpClient, authService)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetProductIdsBySmartGroupIdAsync(string smartGroupId)
        {
            // Not yet implemented
            throw new NotImplementedException();
        }

        public async Task<WorkspaceOneProduct> GetProductByIdAsync(string productId)
        {
            // Not yet implemented
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<WorkspaceOneProduct>> GetProductsBySmartGroupIdAsync(string smartGroupId)
        {
            // Not yet implemented
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<WorkspaceOneProduct>> GetAllProductsAsync()
        {
            var products = new List<WorkspaceOneProduct>();

            try
            {

                await AddAuthHeaderAsync();
                //No params for this call but we pass it anyway:
                var queryParams = new Dictionary<string, string>();
                var obj = await GetPagedResponseAsync("/mdm/products/search", "Products", queryParams, "application/json;version=2");
                

                    foreach (var item in obj)
                    {
                        products.Add(new WorkspaceOneProduct
                        {
                            Id = item["ID"]?["Value"]?.ToString(),
                            Name = item["Name"]?.ToString(),
                            Description = item["Description"]?.ToString(),
                            Platform = item["Platform"]?.ToString(),
                            Status = item["Active"]?.ToString() == "true" ? "Active" : "Inactive",
                            ManagedByOrganizationGroupId = item["ManagedByOrganizationGroupID"]?.ToString(),
                            ManagedByOrganizationGroupName = item["ManagedByOrganizationGroupName"]?.ToString(),
                            TotalAssigned = item["TotalAssigned"]?.ToObject<int>() ?? 0,
                            Compliant = item["Compliant"]?.ToObject<int>() ?? 0,
                            InProgress = item["InProgress"]?.ToObject<int>() ?? 0,
                            Failed = item["Failed"]?.ToObject<int>() ?? 0,
                            ActivationType = item["ActivationOrDeactivationType"]?.ToString(),
                            Version = item["Version"]?.ToObject<int>() ?? 0,
                            DevicePolicyUuid = item["DevicePolicyUuid"]?.ToString(),
                            ProductETag = item["ProductETag"]?.ToString(),
                            SmartGroups = item["SmartGroups"]?.ToObject<List<SmartGroupReference>>()
                        });
                    }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product provisioning profiles from Workspace ONE");
            }

            return products;
        }
    }
}
