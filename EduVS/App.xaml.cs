using EduVS.Helpers;
using EduVS.ViewModels;
using EduVS.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfSharp.Fonts;
using Serilog;
using System.Text;
using System.Windows;

namespace EduVS
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            // GLOBAL SETTINGS
            // font for PDFs
            GlobalFontSettings.FontResolver ??= new DejaVuFontResolver();
            // encoding (for windows-1250)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // SERILOG
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateBootstrapLogger();

            // HOST
            _host = Host.CreateDefaultBuilder()
                // CONFIGURATION
                // host already loads appsettings
                // LOGGING
                .UseSerilog((ctx, services, logger) => logger
                    .ReadFrom.Configuration(ctx.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext())
                // SERVICES
                .ConfigureServices((ctx, services) =>
                {

                    // view models
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<GenerateTestViewModel>();
                    services.AddTransient<PrepareTestCheckViewModel>();
                    services.AddTransient<GenerateTestResultsViewModel>();

                    // view
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<GenerateTestWindowView>();
                    services.AddTransient<PrepareTestCheckWindowView>();
                    services.AddTransient<GenerateTestResultsWindowView>();

                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log.Information("Application starting up");
                _host.Start();

                var mw = _host.Services.GetRequiredService<MainWindow>();
                mw.Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}