using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PhotoSync
{
    public class ValidateConfig
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ValidateConfig(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<ValidateConfig>();
            _configuration = configuration;
        }

        [Function("ValidateConfig")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Configuration validation requested");

            var response = req.CreateResponse();
            
            try
            {
                var isValid = await ConfigurationValidator.ValidateAllConfigurationsAsync(_configuration);
                
                if (isValid)
                {
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync("✓ Configuration is valid. Ready to sync photos!");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("✗ Configuration has errors. Check the logs for details.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
            }

            return response;
        }
    }
}
