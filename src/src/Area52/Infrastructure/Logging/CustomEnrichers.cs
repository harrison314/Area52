using Serilog;
using Serilog.Configuration;

namespace Area52.Infrastructure.Logging;

public static class CustomEnrichers
{
    public static LoggerConfiguration WithPid(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.WithProperty("PID", Environment.ProcessId, false);
    }

    public static LoggerConfiguration WithMachineName(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.WithProperty("MachineName", Environment.MachineName, false);
    }
}
