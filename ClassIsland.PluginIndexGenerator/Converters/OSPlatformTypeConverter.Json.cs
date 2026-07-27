using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OSPlatformConverter_Json : JsonConverter<OSPlatform>
{
    public override OSPlatform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string platformStr = reader.GetString();
        return platformStr switch
        {
            "Windows" => OSPlatform.Windows,
            "Linux" => OSPlatform.Linux,
            "OSX" => OSPlatform.OSX,
            "FreeBSD" => OSPlatform.FreeBSD,
            _ => OSPlatform.Create(platformStr)
        };
    }

    public override void Write(Utf8JsonWriter writer, OSPlatform value, JsonSerializerOptions options)
    {
        string valuestring="";
        if (value == OSPlatform.Windows) valuestring = "Windows";
        else if (value == OSPlatform.Linux) valuestring = "Linux";
        else if (value == OSPlatform.OSX) valuestring = "OSX";
        else if (value == OSPlatform.FreeBSD) valuestring = "FreeBSD";
        else valuestring = value.ToString();
        writer.WriteStringValue(valuestring);
    }
}