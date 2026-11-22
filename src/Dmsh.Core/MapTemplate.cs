using System.Text.Json;

namespace Dmsh.Core;

public sealed record MapTemplate(
    int SchemaVersion, string Template, string Title, bool Overview, bool Coordinates,
    double WorldWidthFeet, string ImagePlacement, GridTemplate Grid, PageTemplate Page)
{
    public static MapTemplate Read(Stream json)
    {
        return JsonSerializer.Deserialize<MapTemplate>(json, JsonSerializerOptions.Web)
            ?? throw new JsonException("Expected a template object.");
    }
}
