# Desktop Map Slicer Headless Tool Suite

A planned .NET 10 command-line tool that turns tabletop battle map images
into printable PDFs.

The first release will accept a local map image and the `default.json`
template from `wmsh_ts/src/default.json`. It will export a PDF with an
overview page followed by four landscape Ledger sheets in a 2×2 layout.

The goal is to offer self-contained Windows and macOS downloads through
GitHub Releases. A future Avalonia app will use the same C# rendering
library as the CLI.
