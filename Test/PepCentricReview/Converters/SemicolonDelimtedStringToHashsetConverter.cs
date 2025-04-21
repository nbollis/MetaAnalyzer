using System.Text.Json;
using System.Text.Json.Serialization;

namespace Test.PepCentricReview;

public class SemicolonDelimitedStringToHashSetConverter : JsonConverter<HashSet<string>>
{
    public override HashSet<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>();

        return new HashSet<string>(raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
    }

    public override void Write(Utf8JsonWriter writer, HashSet<string> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(string.Join(";", value));
    }
}
