# Desktop Map Slicer Headless Tool Suite

A planned .NET 10 command-line tool that turns tabletop battle map images
into printable PDFs.

The first release will accept a local map image and the `default.json`
template from `wmsh_ts/src/default.json`. It will export a PDF with an
overview page followed by four landscape Ledger sheets in a 2×2 layout.

The goal is to offer self-contained Windows and macOS downloads through
GitHub Releases. A future Avalonia app will use the same C# rendering
library as the CLI.

## Show the build version

```powershell
dotnet run --project src/Dmsh.Cli -- --version
```

The CLI shows the application version as `Major.Minor.Build`, the short Git
commit hash, a `Modified` marker for uncommitted changes, and the UTC build time.
Set Major and Minor in `Directory.Build.props`. Build comes from the Git commit
count and falls back to 0 if Git cannot resolve the revision.

## Inspect the sheet layout

From the repository root, pass a template and the map's pixel dimensions:

```sh
dotnet run --project src/Dmsh.Cli -- plan ../wmsh_ts/src/default.json 3200 2000
```

The command prints the planned page count, sheet windows in row order, and
map placement in PDF points. The overview, when enabled, precedes the sheets.
This command does not read the image or export a PDF.

## Export PDFs

PDF export uses PDFsharp. The shared library explicitly references
`System.Security.Cryptography.Pkcs` 8.0.2 to replace the vulnerable 8.0.1
dependency brought in by PDFsharp 6.2.4.

```powershell
dotnet run --project src/Dmsh.Cli -- render map.png --template ../wmsh_ts/src/default.json
dotnet run --project src/Dmsh.Cli -- batch --incoming incoming --output output --template ../wmsh_ts/src/default.json
```

`render` writes `map.pdf` beside `map.png`. `batch` scans the top level of
`incoming` for JPEG, PNG, BMP and WebP files and moves each source into
`incoming/processed` after its PDF is saved in `output`. Existing PDFs,
existing archive paths, and duplicate output names are reported as conflicts.
Batch processing continues after an individual failure and returns exit code 1
if any image failed or was skipped due to a conflict.

The initial exporter draws the clipped map on each sheet and an optional
overview with sheet outlines. Grid artwork, sheet badges, and coordinate
labels are pending.

### Image preprocessing

Both commands check image dimensions before decoding pixels and reject images
over 12,000,000 pixels or with multiple frames. The exporter preserves the
aspect ratio, rounds the scaled dimensions to whole pixels, and caps the
longest edge at 2,400 pixels without upscaling. It averages source pixels
after compositing transparency onto white and embeds a quality 50 JPEG.
The sheet layout uses the resulting JPEG dimensions, as in the TypeScript
exporter. Source image contents stay intact.

The JPEG quality setting is independent of the final PDF file size. SkiaSharp
handles decoding and encoding, so JPEG bytes can differ from TypeScript and
the TypeScript decoder's 80 MB memory ceiling does not apply.
