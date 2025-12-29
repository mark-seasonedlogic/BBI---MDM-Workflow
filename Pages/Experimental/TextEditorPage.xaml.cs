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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextEditorPage : Page
    {
        public TextEditorViewModel ViewModel { get; } = new();

        public TextEditorPage()
        {
            this.InitializeComponent();
        }

        public void LoadFileFromPath(string filePath)
        {
            ViewModel.LoadFile(filePath);
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveFile();
        }
    }

}
