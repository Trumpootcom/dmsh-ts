# Desktop Map Slicer Headless Tool Suite

See [TODO.md](TODO.md) for the remaining porting work.

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

Both export commands print the program version and Git revision, then one
status line per image. Paths are relative to the project root, or to the
working directory when run outside the project.

```text
Processing image: .\map.png -> .\map.pdf
Processing image: incoming\map.png -> output\map.pdf
Processing image: .\bad.png !Bad image file.
```

The arrow means the PDF was saved. If a batch archive move fails after
saving, the same line shows the output path followed by the error.

PDF export uses PDFsharp. The shared library explicitly references
`System.Security.Cryptography.Pkcs` 8.0.2 to replace the vulnerable 8.0.1
dependency brought in by PDFsharp 6.2.4.

```powershell
dotnet run --project src/Dmsh.Cli -- render map.png --template ../wmsh_ts/src/default.json
dotnet run --project src/Dmsh.Cli -- batch --incoming incoming --output output --template ../wmsh_ts/src/default.json
```

`render` writes `map.pdf` beside `map.png`. `batch` scans the top level of
`incoming` for JPEG, PNG, BMP and WebP files and moves each source into
`incoming/processed` after its PDF is saved in `output`. Both commands
replace existing PDFs by default after the new PDF has been written
successfully. Existing archive paths and duplicate output names within a
batch are reported as conflicts.

On Windows, close the existing PDF in Acrobat or another viewer before
rerunning the command if the viewer blocks replacement. If replacement
fails, the existing PDF stays in place.
Batch processing continues after an individual failure and returns exit code 1
if any image failed or was skipped due to a conflict.

The exporter draws the clipped map, the default solid rectangular grid,
and sheet badges such as A1 and B1. On unrotated rectangular sheets, set
`"coordinates": false` in the template JSON to omit the coordinate bars
and their reserved space. The default template enables them. The optional
overview shows the rectangular grid, sheet outlines and their badges, with one set of coordinate
bars around the full map when coordinates are enabled.

On the overview and map sheets, the exporter draws the grid first, then
any coordinate bars, then the page badges so they stay on top.

Coordinate bars use bold row and column labels and thinner distance numbers.
The distance numbers use the same font size and width scaling regardless of
how many digits they contain.

Sheet badges use 18-point bold text with the TypeScript badge's baseline,
padding, and corner placement. Badge text uses Helvetica on macOS and Arial
on Windows.

Regenerate PDFs made by older builds if Acrobat reports a page error. The
exporter now leaves PDF page commands uncompressed and embeds images and
labels as JPEGs to avoid the incomplete compressed streams seen in those
exports.

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
