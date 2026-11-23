internal static class CommandHelp
{
    private const string Plan = "plan <template.json> <image-width-px> <image-height-px>";
    private const string Render = "[--template <template.json>] [--width <feet>] <image>";
    private const string Batch = "batch --incoming <directory> --output <directory> [--template <template.json>] [--width <feet>]";

    public static void WriteOverview(TextWriter output)
    {
        output.WriteLine($"""
            {Dmsh.Core.ApplicationVersion.Current.DisplayName}
            Turn tabletop map images into printable PDF sheets.

            Usage: Dmsh.Cli {Render}
                   Dmsh.Cli <command> [arguments]

              {Render}
                  Export one JPEG, PNG, BMP or static WebP to a PDF beside it.
                  Example: Dmsh.Cli map.png
              {Plan}
                  Print the planned page layout as JSON.
              {Batch}
                  Export images from one folder and archive successful inputs.

              --help, -h      Show this help.
              --version, -v   Show the version, Git revision and UTC build time.

            Run Dmsh.Cli help <command> or Dmsh.Cli <command> --help for examples.
            Exports use the built-in T000.json unless you supply --template.
            Custom templates use schema version 1.
            Use --width <feet> to override the template's worldWidthFeet.
            A filename ending in WxH or WXH supplies its own width in feet.
            Example: Dmsh.Cli "ASHEN-E1-MAP02D_DM 150x110.png" uses 150 feet.
            """);
    }

    public static bool TryWriteCommand(string command, TextWriter output)
    {
        var help = command switch
        {
            "plan" => $"""
                Usage: Dmsh.Cli {Plan}

                Read a schema version 1 template and print the planned layout as JSON.
                Supply the image dimensions in pixels. No image or PDF is opened.

                Example: Dmsh.Cli plan T000.json 2400 1600
                """,
            "batch" => $"""
                Usage: Dmsh.Cli {Batch}

                Process supported images in the top level of the incoming directory.
                Save each PDF in the output directory, then move the source into
                the incoming directory's processed subfolder. Reports per-image
                failures and continues. Returns exit code 1 if any image failed.

                Uses the built-in T000.json unless you supply --template.
                Use --width <feet> to override worldWidthFeet. A trailing WxH
                or WXH in a filename takes precedence for that image.
                Example: Dmsh.Cli batch --incoming incoming --output output
                """,
            _ => null
        };
        if (help is null)
            return false;
        output.WriteLine(help);
        return true;
    }
}
