using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json;
using BBIHardwareSupport.MDM.UI.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OemConfigurationManagerPage : Page
    {

        private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && e.OriginalSource is FrameworkElement fe)
            {
                if (fe.DataContext is JObject config)
                {
                    var flyout = new MenuFlyout();

                    var viewJsonItem = new MenuFlyoutItem { Text = "View JSON" };
                    viewJsonItem.Click += (s, args) => ShowJsonDialog(config);
                    flyout.Items.Add(viewJsonItem);

                    flyout.ShowAt(fe, e.GetPosition(fe));
                }
            }
        }
        private async void ShowJsonDialog(JObject config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            var jsonViewer = new JsonViewerWindow(json);
            jsonViewer.Activate();


        }

        public OemConfigurationManagerPage(OemConfigManagerViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = viewModel;
            this.Loaded += OemConfigurationManagerPage_Loaded;
            
        }
        private void OemConfigurationManagerPage_Loaded(object sender, RoutedEventArgs e)
        {
            ContextMenuHelper.AttachJsonRightClickHandler(OemConfigListView, this.XamlRoot);
        }

    }
}
