using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanFlow.Infrastructure;

/// <summary>Single set of options for writing and reading outbox payloads.</summary>
internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
