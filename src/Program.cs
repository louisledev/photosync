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
        // This is required for .NET 8 isolated worker functions
        // In .NET 8 isolated worker functions, Application Insights uses a default filter that blocks Information-level logs. Removing this filter ensures all logs are captured. See: https://stackoverflow.com/questions/77565541/logs-not-appearing-in-application-insights-for-azure-functions-v4-with-net-8
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
