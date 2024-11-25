using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.UnitTests.Helpers
{
    public class MockLogger<T> : ILogger<T>
    {
        private readonly List<string> _logMessages = [];

        public IReadOnlyList<string> LogMessages => _logMessages;

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logMessages.Add(formatter(state, exception));
        }
    }
}
