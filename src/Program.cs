using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    .Build();

host.Run();
