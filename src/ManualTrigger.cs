using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace PhotoSync
{
    public class ManualTrigger
    {
        private readonly ILogger _logger;
        private readonly IPhotoSyncService _photoSyncService;

        public ManualTrigger(ILoggerFactory loggerFactory, IPhotoSyncService photoSyncService)
        {
            _logger = loggerFactory.CreateLogger<ManualTrigger>();
            _photoSyncService = photoSyncService;
        }

        [Function("ManualSync")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Manual sync triggered via HTTP");

            try
            {
                await _photoSyncService.SyncPhotosAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Photo sync completed successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual sync");

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}
