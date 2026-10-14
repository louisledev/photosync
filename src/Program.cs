using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhotoSync;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register HttpClient for refresh token authentication
        services.AddHttpClient();

        // Register custom services
        services.AddSingleton<IPhotoSyncService, PhotoSyncService>();
        services.AddSingleton<IStateManager, StateManager>();
        services.AddSingleton<IGraphClientFactory>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new GraphClientFactory(configuration, httpClientFactory.CreateClient());
        });

        // Add configuration
        services.AddSingleton<IConfiguration>(context.Configuration);
    })
    .ConfigureLogging(logging =>
    {
        // Remove default logging filters to allow Application Insights to capture all logs
        // Application Insights defaults to Warning and higher; remove its filter to capture Information-level logs.
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            foreach (var appInsightRule in options.Rules.Where(rule =>
                rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider").ToList())
            {
                options.Rules.Remove(appInsightRule);
            }
        });
    })
    .Build();

host.Run();
