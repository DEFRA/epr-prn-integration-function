using EprPrnIntegration.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api
{
    public class PrnFunction
    {
        private readonly ILogger<PrnFunction> _logger;
        private readonly HttpClient _httpClient;

        public PrnFunction(IHttpClientFactory httpClientFactory, ILogger<PrnFunction> logger)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(EprPrnIntegration.Common.Constants.HttpClientNames.Npwd);
        }

        [Function("PrnFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                var response = await _httpClient.GetAsync("oData/PRNs");
                response.EnsureSuccessStatusCode();
                return new OkObjectResult(response.Content.ToString());

                //var res = new[]
                //{
                //    new PrnDetails
                //    {
                //        AccreditationNo = "ER212020210",
                //        AccreditationYear = 2021,
                //        CancelledDate = DateTimeOffset.Parse("2021-11-24T13:58:23.997Z").UtcDateTime,
                //        DecemberWaste = false,
                //        EvidenceMaterial = "Plastic",
                //        EvidenceNo = "ER2113680U",
                //        EvidenceStatusCode = "EV-CANCEL",
                //        EvidenceStatusDesc = "Cancelled",
                //        EvidenceTonnes = 600,
                //        IssueDate = DateTimeOffset.Parse("2021-09-29T15:49:47.933+01:00").UtcDateTime,
                //        IssuedByNPWDCode = "NPWD342652",
                //        IssuedByOrgName = "R M Polymers Ltd",
                //        IssuedToEPRCode = "616",
                //        IssuedToEPRId = "7666ff23-db56-47b9-a3d4-2d40e407d570",
                //        IssuedToNPWDCode = "NPWD101877",
                //        IssuedToOrgName = "Wastepack EA",
                //        IssuerNotes = "Clarity PO1692",
                //        IssuerRef = "",
                //        MaterialOperationCode = "R-PLA",
                //        ModifiedOn = DateTimeOffset.Parse("2021-11-24T13:58:23.997Z").UtcDateTime,
                //        ObligationYear = 2025,
                //        PRNSignatory = "Vicki Cooper",
                //        PRNSignatoryPosition = null,
                //        ProducerAgency = "Environment Agency",
                //        RecoveryProcessCode = "R3",
                //        ReprocessorAgency = "Environment Agency",
                //        StatusDate = DateTimeOffset.Parse("2021-11-24T13:58:23.997Z").UtcDateTime
                //    }
                //};

               // return new OkObjectResult(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(message: ex.Message, exception: ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
