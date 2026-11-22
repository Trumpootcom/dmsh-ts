namespace Dmsh.Core;

public sealed record GridTemplate(
    string Style, bool Iso, double? IsoAngleDegrees, double RotationDegrees,
    double OffsetX, double OffsetY, double InchesPerFiveFeet, string LineStyle,
    string Color, double LineWidthPoints, double Opacity, string Extent);

public sealed record PageTemplate(
    string Size, string Orientation, int Rows, int Columns, double MarginInches, double OverlapInches);

public sealed record SheetWindow(int Row, int Column, double Left, double Top);

public sealed record SheetPlan(
    double PageWidth, double PageHeight, double Margin, double Overlap,
    double PrintableWidth, double PrintableHeight, double CanvasWidth, double CanvasHeight,
    double GridSpacing, double MapLeft, double MapTop, double MapWidth, double MapHeight,
    bool HasCoordinateBorder, bool HasOverview, IReadOnlyList<SheetWindow> Sheets)
{
    public int PageCount => Sheets.Count + (HasOverview ? 1 : 0);

    // All geometry uses PDF points, with canvas coordinates measured from the top left.
    public static SheetPlan Create(MapTemplate template, int imageWidth, int imageHeight)
    {
        if (template.SchemaVersion != 1 || template.ImagePlacement != "center"
            || template.Grid is not { } grid || template.Page is not { } page)
            throw new ArgumentException("Expected a schema version 1 template with centered placement, grid and page settings.");

        var (shortEdge, longEdge) = page.Size switch
        {
            "letter" => (612d, 792d),
            "ledger" => (792d, 1224d),
            _ => throw new ArgumentException("Paper size must be letter or ledger.")
        };
        var (width, height) = page.Orientation switch
        {
            "portrait" => (shortEdge, longEdge),
            "landscape" => (longEdge, shortEdge),
            _ => throw new ArgumentException("Orientation must be portrait or landscape.")
        };
        if (page.Rows < 1 || page.Columns < 1 || (long)page.Rows * page.Columns > 100)
            throw new ArgumentException("The page array must contain between 1 and 100 sheets.");

        const double pointsPerInch = 72;
        var margin = page.MarginInches * pointsPerInch;
        var overlap = page.OverlapInches * pointsPerInch;
        if (!double.IsFinite(margin) || margin < 0 || 2 * margin >= shortEdge)
            throw new ArgumentException("Margin must be nonnegative and leave printable area.");
        if (!double.IsFinite(overlap) || overlap < 0 || overlap >= shortEdge - 2 * margin)
            throw new ArgumentException("Overlap must be nonnegative and smaller than the printable sheet dimensions.");

        var printableWidth = width - 2 * margin;
        var printableHeight = height - 2 * margin;
        var stepX = printableWidth - overlap;
        var stepY = printableHeight - overlap;
        var canvasWidth = printableWidth + (page.Columns - 1) * stepX;
        var canvasHeight = printableHeight + (page.Rows - 1) * stepY;
        var spacing = grid.InchesPerFiveFeet * pointsPerInch;
        var requestedWidth = template.WorldWidthFeet / 5 * spacing;
        if (!double.IsFinite(spacing) || spacing < 0.25 || !double.IsFinite(requestedWidth)
            || requestedWidth <= 0 || imageWidth <= 0 || imageHeight <= 0)
            throw new ArgumentException("Grid spacing, map scale or image dimensions are invalid.");

        var coordinates = template.Coordinates && grid.Style != "hex" && !grid.Iso && grid.RotationDegrees == 0;
        // Each of the two opposing coordinate bars reserves half a grid cell.
        var reserve = coordinates ? spacing : 0;
        var aspect = (double)imageHeight / imageWidth;
        var mapWidth = Math.Min(requestedWidth, Math.Min(canvasWidth - reserve, (canvasHeight - reserve) / aspect));
        if (mapWidth <= 0)
            throw new ArgumentException("The fitted map has no room on the printable canvas.");
        var mapHeight = mapWidth * aspect;

        var sheets = new List<SheetWindow>();
        for (var row = 0; row < page.Rows; row++)
            for (var column = 0; column < page.Columns; column++)
                sheets.Add(new SheetWindow(row, column, column * stepX, row * stepY));

        return new SheetPlan(width, height, margin, overlap, printableWidth, printableHeight,
            canvasWidth, canvasHeight, spacing, (canvasWidth - mapWidth) / 2,
            (canvasHeight - mapHeight) / 2, mapWidth, mapHeight, coordinates, template.Overview,
            sheets.AsReadOnly());
    }
}
