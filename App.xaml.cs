using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.IntuneConfigManager.Pages;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.Services.Authentication;
using BBIHardwareSupport.MDM.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using NLog;
using NLog.Extensions.Logging;
using System;

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

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });

            // ──────────────────────────────────────────────────────────────
            // CORE (currently active)
            // ──────────────────────────────────────────────────────────────

            // Graph auth (single registration)
            services.AddSingleton<IGraphAuthService>(_ =>
                new GraphAuthService(
                    "dd656aba-7b3a-4606-a1d5-b1d05cad986b",
                    "c937126d-5291-47ed-8e01-3cb0fd4e1dfb",
                    Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")));

            services.AddSingleton<IGraphADDeviceService, GraphEnrolledDeviceService>();
            services.AddSingleton<IGraphIntuneDeviceService, GraphManagedDeviceService>();
            services.AddSingleton<IGraphADGroupService, GraphGroupService>();
            services.AddSingleton<IGraphDeviceUpdater, GraphDeviceUpdater>();
            services.AddHttpClient<IWorkspaceOneAdminsService, WorkspaceOneAdminsService>();

            services.AddSingleton<MainViewModel>();

            // Keep pages (DI only needed if you resolve via GetRequiredService<Page>())
            services.AddTransient<WorkspaceOnePage>();
            services.AddTransient<WorkspaceOneViewModel>();

            services.AddTransient<OemConfigurationManagerPage>();
            services.AddTransient<OemConfigManagerViewModel>();

            // If WS1AndroidBatteryPage has a VM and you resolve via DI, register here:
            // services.AddTransient<WS1AndroidBatteryPage>();
            // services.AddTransient<WS1AndroidBatteryViewModel>();

            // Workspace ONE services
            services.AddSingleton<IWorkspaceOneAuthService, WorkspaceOneAuthService>();
            services.AddSingleton<IApiAuthService, WorkspaceOneAuthService>();

            services.AddTransient<IWorkspaceOneDeviceService, WorkspaceOneDeviceService>();
            services.AddHttpClient<IWorkspaceOneSmartGroupsService, WorkspaceOneSmartGroupsService>();
            services.AddScoped<IProductsService, WorkspaceOneProductsService>();
            services.AddScoped<IWorkspaceOneProfileService, WorkspaceOneProfileService>();
            services.AddHttpClient<IWorkspaceOneTaggingService, WorkspaceOneTaggingService>();

            services.AddSingleton<IJournalService, FileJournalService>();
            services.AddSingleton<IWorkspaceOneGraphService, WorkspaceOneGraphServiceAdapter>();

            // Graph/Intune configuration services (keep if still used)
            services.AddScoped<IGraphIntuneConfigurationService, GraphIntuneConfigurationService>();
            services.AddScoped<IGraphIntuneManagedAppService, GraphIntuneManagedAppService>();
            services.AddScoped<IGraphDeviceCategoryService, GraphDeviceCategoryService>();

            services.AddHttpClient();

            // ──────────────────────────────────────────────────────────────
            // PARKED (pages/features moved to Pages\Experimental)
            // ──────────────────────────────────────────────────────────────
            #region Parked_ExperimentalPagesAndFeatures
            /*
            services.AddSingleton<IntuneGroupsPageViewModel>();
            services.AddSingleton<GitManagerViewModel>();

            services.AddScoped<IAppConfigTemplateHelper, AppConfigTemplateHelper>();
            services.AddScoped<ISchemaExtensionRegistrar, SchemaExtensionRegistrarService>();
            services.AddTransient<SchemaAdminViewModel>();
            services.AddTransient<SchemaExtensionAdminPage>();

            services.AddTransient<IntuneGroupsPage>();

            services.AddTransient<GraphEditorViewModel>();
            services.AddTransient<GraphEditorPage>();
            */
            #endregion

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
