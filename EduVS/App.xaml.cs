using EduVS.Data;
using EduVS.ViewModels;
using EduVS.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EduVS
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private IServiceScope? _uiScope;

        public App()
        {
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

                    services.AddScoped<TestsViewModel>();
                    services.AddScoped<StudentsViewModel>();
                    services.AddScoped<MainViewModel>();
                    services.AddScoped<MainWindow>();
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
                    db.Database.EnsureCreated();

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
