# Fake host secrets

Mounting this file into the azure function container enables it to run with a known master key: required to invoke the functions during testing [example](/src/EprPrnIntegration.Api.IntegrationTests/AzureFunctionInvokerContext.cs).

