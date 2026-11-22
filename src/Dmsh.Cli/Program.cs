using Dmsh.Core;

if (args is ["--version"] or ["-v"])
{
    Console.WriteLine(ApplicationVersion.Current.DisplayName);
    Console.WriteLine($"Built: {ApplicationVersion.Current.BuildTimestampUtc?.ToString("u") ?? "unknown"}");
    return;
}

Console.WriteLine("Desktop Map Slicer CLI");
Console.WriteLine("Use --version to display build information.");
