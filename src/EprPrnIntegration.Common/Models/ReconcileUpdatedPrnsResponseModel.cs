﻿using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Models;

public class ReconcileUpdatedPrnsResponseModel
{
    [JsonProperty(nameof(PrnNumber))]
    public string PrnNumber { get; set; } = null!;

    [JsonProperty(nameof(StatusName))]
    public string StatusName { get; set; } = null!;

    [JsonProperty(nameof(UpdatedOn))]
    public string UpdatedOn { get; set; } = null!;

    [JsonProperty(nameof(OrganisationName))]
    public string OrganisationName { get; set; } = null!;
}
