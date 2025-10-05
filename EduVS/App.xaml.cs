using EduVS.Data;
using EduVS.ViewModels;
using EduVS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfSharp.Fonts;
using Serilog;
using System.IO;
using System.Text;
using System.Windows;

namespace EduVS
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private IServiceScope? _uiScope;

        public App()
        {
            //var s0 = @"C:\Users\Jensa\Desktop\test_snimek_0.png";
            //var s180 = @"C:\Users\Jensa\Desktop\test_snimek_180.png";

            //using PaddleRotationDetector detector = new PaddleRotationDetector(RotationDetectionModel.EmbeddedDefault);
            //using Mat src = Cv2.ImRead(s0);
            //RotationResult r = detector.Run(src);
            //Debug.WriteLine($"{s0}: {r.Rotation}");

            // GLOBAL SETTINGS
            // font for PDFs
            GlobalFontSettings.FontResolver ??= new DejaVuFontResolver();
            // encoding (for windows-1250)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // SERILOG
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();

            // HOST
            _host = Host.CreateDefaultBuilder()
                // CONFIG FILES
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment.EnvironmentName;
                    cfg.AddJsonFile($"appsettings.{env}.json", optional: false, reloadOnChange: true);

                }
                )
                // LOGGING
                .UseSerilog()
                // SERVICES
                .ConfigureServices((ctx, services) =>
                {
                    // DB
                    services.AddDbContext<AppDbContext>((provider, opts) =>
                    {
                        var cfg = provider.GetRequiredService<IConfiguration>();
                        opts.UseSqlite(cfg.GetConnectionString("Default"));
                    });

                    // view models
                    services.AddScoped<MainViewModel>();
                    services.AddScoped<GenerateTestViewModel>();
                    services.AddScoped<PrepareTestCheckViewModel>();
                    services.AddScoped<GenerateTestResultsViewModel>();

                    // view
                    services.AddScoped<MainWindow>();
                    services.AddTransient<GenerateTestWindowView>();
                    services.AddTransient<PrepareTestCheckWindowView>();
                    services.AddTransient<GenerateTestResultsWindowView>();

                    // other
                    // services.AddSingleton<PdfManager>();
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log.Information("Application starting up");
                _host.Start();

                // create db
                using (var migrationScope = _host.Services.CreateScope())
                {
                    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();

                    var conn = db.Database.GetDbConnection();
                    Log.Information("DB connection created at: {Path}", Path.GetFullPath(conn.DataSource ?? ""));
                }

                // create scope
                _uiScope = _host.Services.CreateScope();
                var mw = _uiScope.ServiceProvider.GetRequiredService<MainWindow>();
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
            _uiScope?.Dispose();
            _host.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
