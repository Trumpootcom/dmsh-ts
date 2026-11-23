using Dmsh.Core;
using System.Text.Json;

if (args is ["--version"] or ["-v"])
{
    Console.WriteLine(ApplicationVersion.Current.DisplayName);
    Console.WriteLine($"Built: {ApplicationVersion.Current.BuildTimestampUtc?.ToString("u") ?? "unknown"}");
    return;
}

if (args is ["plan", var path, var width, var height]
    && int.TryParse(width, out var imageWidth) && int.TryParse(height, out var imageHeight))
{
    try
    {
        using var json = File.OpenRead(path);
        var plan = SheetPlan.Create(MapTemplate.Read(json), imageWidth, imageHeight);
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions(JsonSerializerOptions.Web)
        {
            WriteIndented = true
        }));
        return;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
    {
        Console.Error.WriteLine(error.Message);
        Environment.ExitCode = 1;
        return;
    }
}

try
{
    if (args is ["render", var image, "--template", var renderTemplate])
    {
        Console.WriteLine(ApplicationVersion.Current.DisplayName);
        Environment.ExitCode = RenderCommands.Render(image, renderTemplate);
        return;
    }

    if (args is ["batch", "--incoming", var incoming, "--output", var output, "--template", var batchTemplate])
    {
        Console.WriteLine(ApplicationVersion.Current.DisplayName);
        Environment.ExitCode = RenderCommands.Batch(incoming, output, batchTemplate);
        return;
    }
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("Desktop Map Slicer CLI");
Console.WriteLine("  plan <template.json> <image-width-px> <image-height-px>");
Console.WriteLine("  render <image> --template <template.json>");
Console.WriteLine("  batch --incoming <directory> --output <directory> --template <template.json>");
Console.WriteLine("  --version");
Environment.ExitCode = args.Length == 0 ? 0 : 2;
