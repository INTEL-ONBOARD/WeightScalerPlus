using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WeightMaster.Services;

namespace WeightMaster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
            {
                DefaultValue = FindResource(typeof(Window))
            });

            // ---- File logging (log.json next to the EXE) --------------------------------------
            // Stamp every line with the app version (injected at build) and the station branch.
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            string branch = "";
            try { branch = new WeightMaster.Config.AppConfig().branchName; } catch { /* config.json may be absent */ }
            Logger.Init(version, branch);
            Logger.Event("app_started", new { version, branch });

            // ---- Cloud mirror -----------------------------------------------------------------
            // Entirely inert unless cloudsync.json is present with sync_enabled true.
            // Runs on a background thread and cannot throw into startup, so a branch
            // without that file behaves exactly as it does today.
            WeightMaster.Services.Sync.SyncBootstrap.StartInBackground();

            // Capture every unhandled error app-wide so nothing fails silently again.
            // (We only OBSERVE here — we don't change whether the app crashes/continues.)
            DispatcherUnhandledException += (s, e) =>
                Logger.Error("UI.Dispatcher", "Unhandled UI exception", e.Exception);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Logger.Error("AppDomain", "Unhandled fatal exception", e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logger.Error("TaskScheduler", "Unobserved task exception", e.Exception);
                e.SetObserved();
            };
        }
    }

}
