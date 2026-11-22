using Dmsh.Core;

internal static class RenderCommands
{
    public static void Render(string image, string templatePath)
    {
        var template = ReadTemplate(templatePath);
        var pdf = Path.ChangeExtension(Path.GetFullPath(image), ".pdf");
        Save(template, image, pdf);
        Console.WriteLine(pdf);
    }

    public static int Batch(string incoming, string output, string templatePath)
    {
        var template = ReadTemplate(templatePath);
        incoming = Path.GetFullPath(incoming);
        output = Path.GetFullPath(output);
        var processed = Path.Combine(incoming, "processed");
        var groups = Directory.GetFiles(incoming).Where(IsImage)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(processed);

        var exitCode = 0;
        foreach (var group in groups)
        {
            if (group.Count() > 1)
            {
                Console.Error.WriteLine($"Multiple images would produce {group.Key}.pdf. Skipped: {string.Join(", ", group)}");
                exitCode = 1;
                continue;
            }

            var image = group.Single();
            var pdf = Path.Combine(output, Path.ChangeExtension(Path.GetFileName(image), ".pdf"));
            var archivedImage = Path.Combine(processed, Path.GetFileName(image));
            var saved = false;
            try
            {
                if (File.Exists(archivedImage) || Directory.Exists(archivedImage))
                    throw new IOException($"Processed path already exists: {archivedImage}");

                Save(template, image, pdf);
                saved = true;
                File.Move(image, archivedImage);
                Console.WriteLine($"{pdf} (source moved to {archivedImage})");
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"{image}: {error.Message}");
                if (saved)
                    Console.Error.WriteLine($"PDF saved to {pdf}. The source could not be archived.");
                exitCode = 1;
            }
        }
        return exitCode;
    }

    private static bool IsImage(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp";

    private static MapTemplate ReadTemplate(string path)
    {
        using var json = File.OpenRead(path);
        return MapTemplate.Read(json);
    }

    private static void Save(MapTemplate template, string image, string pdf)
    {
        if (!IsImage(image))
            throw new ArgumentException("Supported images are JPEG, PNG, BMP and static WebP.");
        if (File.Exists(pdf) || Directory.Exists(pdf))
            throw new IOException($"Output path already exists: {pdf}");

        var temporary = Path.Combine(Path.GetDirectoryName(pdf)!, $".dmsh-{Guid.NewGuid():N}.tmp");
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
        {
            try
            {
                MapPdfExporter.Export(template, image, stream);
                stream.Flush(flushToDisk: true);
            }
            catch
            {
                stream.Dispose();
                File.Delete(temporary);
                throw;
            }
        }
        try
        {
            File.Move(temporary, pdf);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
