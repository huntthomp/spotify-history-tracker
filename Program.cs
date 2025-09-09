
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Auralytics.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auralytics
{
    static class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                Env.Load(".env");

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                string connectionString = configuration["ConnectionStrings:DefaultConnection"]!;

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString));

                services.AddSingleton<ISpotifyTokenRefresher, SpotifyTokenRefreshService>();
                services.AddSingleton<ISpotifyAPIInterface, SpotifyAPIInterface>();

                services.AddSingleton<IHostedService>(provider =>
                    (SpotifyTokenRefreshService)provider.GetRequiredService<ISpotifyTokenRefresher>());

                services.AddSingleton<IHostedService>(provider =>
                    (SpotifyAPIInterface)provider.GetRequiredService<ISpotifyAPIInterface>());

                // Register the background service for scheduled tasks
                services.AddHostedService<SpotifyHistoryUpdateService>();

                services.AddRouting();
            }).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    var routeBuilder = new RouteBuilder(app);

                    routeBuilder.MapGet("health", context =>
                    {
                        context.Response.StatusCode = 200;
                        return context.Response.WriteAsync("OK");
                    });

                    var routes = routeBuilder.Build();
                    app.UseRouter(routes);
                })
                .UseUrls($"http://*:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");
            });
    }
}