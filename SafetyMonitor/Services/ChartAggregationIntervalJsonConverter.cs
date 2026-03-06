using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents chart aggregation interval json converter and encapsulates its related behavior and state.
/// </summary>
public sealed class ChartAggregationIntervalJsonConverter : JsonConverter<TimeSpan> {
    /// <summary>
    /// Executes read as part of chart aggregation interval json converter processing.
    /// </summary>
    /// <param name="reader">Input value for reader.</param>
    /// <param name="typeToConvert">Input value for type to convert.</param>
    /// <param name="options">Input value for options.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Executes write as part of chart aggregation interval json converter processing.
    /// </summary>
    /// <param name="writer">Input value for writer.</param>
    /// <param name="value">Input value for value.</param>
    /// <param name="options">Input value for options.</param>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WriteStringValue(ChartAggregationHelper.FormatAggregationLabel(value));
    }
}
