using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafetyMonitor.Services;

public sealed class ChartAggregationIntervalJsonConverter : JsonConverter<TimeSpan> {
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.String) {
            var text = reader.GetString();
            if (ChartAggregationHelper.TryParseAggregationLabel(text, out var interval)) {
                return interval;
            }
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var seconds)) {
            return ChartAggregationHelper.NormalizeAggregationInterval(TimeSpan.FromSeconds(seconds));
        }

        throw new JsonException("Invalid chart aggregation interval format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WriteStringValue(ChartAggregationHelper.FormatAggregationLabel(value));
    }
}
