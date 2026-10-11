using Microsoft.Extensions.Logging;
using RxLinkApi.Logging;

namespace Application.Core.Services.Tests.Logging;

public class StructuredResultLoggerTests
{
    private sealed record PatientDto(string FullName, string DocumentNumber);

    private sealed record SampleError(string Message);

    private sealed class CapturingLogger : ILogger<StructuredResultLogger>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception));
    }

    [Fact]
    public void LogSuccess_DoesNotWritePayload()
    {
        var sink = new CapturingLogger();
        new StructuredResultLogger(sink).LogSuccess("GetPatient", new PatientDto("Juan Perez", "12345678"));

        string line = Assert.Single(sink.Lines);
        Assert.Contains("GetPatient", line);
        Assert.DoesNotContain("Juan", line);
        Assert.DoesNotContain("12345678", line);
    }

    [Fact]
    public void LogError_WritesTypeNameOnly()
    {
        var sink = new CapturingLogger();
        new StructuredResultLogger(sink).LogError("GetPatient", new SampleError("Key (DocumentNumber)=(12345678)"));

        string line = Assert.Single(sink.Lines);
        Assert.Contains(nameof(SampleError), line);
        Assert.DoesNotContain("12345678", line);
    }
}
