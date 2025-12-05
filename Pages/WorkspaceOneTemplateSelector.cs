using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Pages
{
    /// <summary>
    /// Selects an appropriate <see cref="DataTemplate"/> for an item in a ListView
    /// based on the item's runtime type. This enables a single ListView to display
    /// multiple artifact types (e.g., devices, SmartGroups) without needing separate controls.
    /// </summary>
    /// <remarks>
    /// This pattern supports a flexible and reusable UI architecture, reducing duplication
    /// while allowing each artifact type to maintain its own display logic.
    /// </remarks>
    public class WorkspaceOneTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template used to render <see cref="WorkspaceOneDevice"/> items.
        /// </summary>
        public DataTemplate DeviceTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used to render <see cref="WorkspaceOneSmartGroup"/> items.
        /// </summary>
        public DataTemplate SmartGroupTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used to render <see cref="WorkspaceOneProduct"/> items.
        /// </summary>
        public DataTemplate ProductTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used to render <see cref="WorkspaceOneProfile"/> items.
        /// </summary>
        public DataTemplate ProfileTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used to render <see cref="WorkspaceOneTimeZoneTagReview"/> items.
        /// </summary>
        public DataTemplate TimeZoneTagReviewTemplate { get; set; }

        /// <summary>
        /// Returns the appropriate <see cref="DataTemplate"/> based on the type of the item.
        /// This overload is called internally by the framework when rendering items.
        /// </summary>
        /// <param name="item">The data item to be rendered.</param>
        /// <returns>The appropriate <see cref="DataTemplate"/> for the item type.</returns>
        protected override DataTemplate SelectTemplateCore(object item)
        {
            // Match on known model types and return the appropriate template
            return item switch
            {
                WorkspaceOneDevice => DeviceTemplate,
                WorkspaceOneSmartGroup => SmartGroupTemplate,
                WorkspaceOneProduct => ProductTemplate,
                WorkspaceOneProfileSummary => ProfileTemplate,
                TimeZoneTagReviewRow => TimeZoneTagReviewTemplate,
                _ => base.SelectTemplateCore(item)
            };
        }

        /// <summary>
        /// Optionally supports access to the container as well.
        /// This overload is required when using XAML-based selectors in WinUI 3.
        /// </summary>
        /// <param name="item">The data item to be rendered.</param>
        /// <param name="container">The container that will display the item.</param>
        /// <returns>The appropriate <see cref="DataTemplate"/> for the item type.</returns>
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            // Delegate to type-only selector
            return SelectTemplateCore(item);
        }
    }
}
