
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Test.PepCentricReview;

public class DoubleToStringConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString();
            if (double.TryParse(value, out double result))
            {
                return result;
            }
        }
        return 0; // Or handle invalid cases as needed
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
