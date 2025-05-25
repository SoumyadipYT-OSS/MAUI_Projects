using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace CalculatorApp {
    public static class MauiProgram {
        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                // Configure lifecycle events for better performance
                .ConfigureLifecycleEvents(events => {
#if ANDROID
                    events.AddAndroid(android => android
                        .OnCreate((activity, bundle) => OnActivityCreated(activity))
                        .OnStop(activity => OnActivityStopped(activity))
                        .OnResume(activity => OnActivityResumed(activity)));
#elif IOS
                    events.AddiOS(ios => ios
                        .OnActivated(app => OnActivated())
                        .OnResignActivation(app => OnResignActivation()));
#endif
                })
                // Configure essential services only - avoid registering unnecessary services
                .ConfigureEssentialServices();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

#if ANDROID
        private static void OnActivityCreated(Android.App.Activity activity) {
            // Ensure activity.Window is not null before accessing it
            // Configure hardware acceleration for better rendering performance
            activity.Window?.SetFlags(
                Android.Views.WindowManagerFlags.HardwareAccelerated,
                Android.Views.WindowManagerFlags.HardwareAccelerated);
        }

        private static void OnActivityStopped(Android.App.Activity activity) {
            // Free resources when app goes to background
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

        private static void OnActivityResumed(Android.App.Activity activity) {
            // Any performance optimizations when app resumes
        }
#elif IOS
        private static void OnActivated()
        {
            // App activated optimizations
        }

        private static void OnResignActivation()
        {
            // Free resources when app goes to background
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
#endif

        // Extension method to configure only essential services
        private static MauiAppBuilder ConfigureEssentialServices(this MauiAppBuilder builder) {
            // Register only the services your calculator actually uses
            // This avoids unnecessary initialization of unused services

            return builder;
        }
    }
}