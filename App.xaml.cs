using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using NLog;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using Microsoft.Extensions.DependencyInjection;

using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using Windows.Devices.WiFiDirect.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static Window MainWindow { get; private set; }
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            LogManager.LoadConfiguration("NLog.config");
            Services = ConfigureServices();
            Logger.Info("Application started!");
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register services and view models
            services.AddSingleton<IGraphAuthService, GraphAuthService>();
            services.AddSingleton<IGraphADDeviceService, GraphEnrolledDeviceService>();
            services.AddSingleton<IGraphIntuneDeviceService, GraphManagedDeviceService>();
            services.AddSingleton<IGraphADGroupService, GraphGroupService>();
            services.AddSingleton<IGraphDeviceUpdater, GraphDeviceUpdater>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IntuneGroupsPageViewModel>();
            services.AddSingleton<GitManagerViewModel>();
            services.AddScoped<OemConfigManagerViewModel>();
            services.AddScoped<OemConfigurationManagerPage>();
            services.AddScoped<IGraphIntuneConfigurationService, GraphIntuneConfigurationService>();
            services.AddScoped<IGraphIntuneManagedAppService, GraphIntuneManagedAppService>();
            services.AddScoped<IGraphDeviceCategoryService, GraphDeviceCategoryService>();

            services.AddHttpClient();
            services.AddScoped<IAppConfigTemplateHelper, AppConfigTemplateHelper>();
            services.AddScoped<ISchemaExtensionRegistrar, SchemaExtensionRegistrarService>();
            services.AddTransient<SchemaAdminViewModel>();
            services.AddTransient<SchemaExtensionAdminPage>();
            services.AddTransient<IntuneGroupsPage>();






            return services.BuildServiceProvider();
        }
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }


    }
}
