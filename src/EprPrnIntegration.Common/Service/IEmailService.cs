﻿using EprPrnIntegration.Api.Models;

namespace EprPrnIntegration.Common.Service;

public interface IEmailService
{
    void SendEmailsToProducers(List<ProducerEmail> producerEmails, string organisationId);
}