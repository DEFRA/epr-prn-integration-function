using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Functions;

public class ErrorQueueFunction(ILogger<ErrorQueueFunction> logger)
{
    [Function(nameof(ErrorQueueFunction))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBus:ErrorPrnQueue%")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Message ID: {id}", message.MessageId);
        logger.LogInformation("Message Body: {body}", message.Body);
        logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);





        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}