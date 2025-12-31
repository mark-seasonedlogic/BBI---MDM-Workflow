using System.Collections.Generic;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    /// <summary>
    /// Provides access to product provisioning assignments and metadata from Workspace ONE.
    /// </summary>
    public interface IProductsService
    {
        /// <summary>
        /// Retrieves all product provisioning profiles assigned to the specified SmartGroup.
        /// </summary>
        /// <param name="smartGroupId">The Workspace ONE SmartGroup ID.</param>
        /// <returns>A list of basic product references (IDs or names).</returns>
        Task<IEnumerable<string>> GetProductIdsBySmartGroupIdAsync(string smartGroupId);

        /// <summary>
        /// Retrieves detailed product information for a given product ID.
        /// </summary>
        /// <param name="productId">The Workspace ONE Product Provisioning ID.</param>
        /// <returns>A WorkspaceOneProduct object describing the configuration, apps, and actions.</returns>
        Task<WorkspaceOneProduct> GetProductByIdAsync(string productId);

        /// <summary>
        /// Retrieves all product details assigned to the given SmartGroup.
        /// </summary>
        /// <param name="smartGroupId">The Workspace ONE SmartGroup ID.</param>
        /// <returns>A list of full product objects.</returns>
        Task<IEnumerable<WorkspaceOneProduct>> GetProductsBySmartGroupIdAsync(string smartGroupId);

        /// <summary>
        /// Retrieves all product provisioning profiles in the Workspace ONE environment.
        /// </summary>
        /// <returns>A list of all Workspace ONE products.</returns>
        Task<IEnumerable<WorkspaceOneProduct>> GetAllProductsAsync();
    }
}
