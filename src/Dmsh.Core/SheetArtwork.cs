using PdfSharp.Drawing;

namespace Dmsh.Core;

internal static class SheetArtwork
{
    private static readonly XSolidBrush BorderLight = new(XColor.FromArgb(240, 235, 219));
    private static readonly XSolidBrush BorderDark = new(XColor.FromArgb(186, 177, 151));
    private static readonly XPen BorderPen = new(XColor.FromArgb(89, 74, 46), 0.5);
    // TypeScript coordinateBarStyle HSL colors, rounded to RGB bytes.
    private static readonly XColor CoordinateInk = XColor.FromArgb(46, 36, 20);
    private static readonly XColor DistanceInk = XColor.FromArgb(97, 79, 51);

    public static void DrawGrid(XGraphics graphics, GridTemplate grid, SheetPlan plan, SheetWindow sheet)
        => DrawGrid(graphics, grid, plan, sheet.Left, sheet.Top,
            new XRect(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight));

    public static void DrawOverviewGrid(XGraphics graphics, GridTemplate grid, SheetPlan plan)
        => DrawGrid(graphics, grid, plan, 0, 0, new XRect(0, 0, plan.CanvasWidth, plan.CanvasHeight));

    private static void DrawGrid(XGraphics graphics, GridTemplate grid, SheetPlan plan,
        double sheetLeft, double sheetTop, XRect bounds)
    {
        if (grid.Style is not ("rect" or "square") || grid.Iso || grid.RotationDegrees != 0
            || grid.LineStyle != "solid" || grid.Opacity <= 0 || grid.Color is "diff" or "contrast")
            return;

        var color = grid.Color switch { "white" => 255, "gray" => 128, _ => 0 };
        var pen = new XPen(XColor.FromArgb((int)Math.Round(Math.Clamp(grid.Opacity, 0, 1) * 255),
            color, color, color), grid.LineWidthPoints);
        var spacing = plan.GridSpacing;
        var x = FirstLine(plan.CanvasWidth / 2 + grid.OffsetX % 1 * spacing, sheetLeft, spacing);
        var y = FirstLine(plan.CanvasHeight / 2 + grid.OffsetY % 1 * spacing, sheetTop, spacing);
        for (; x <= sheetLeft + bounds.Width; x += spacing)
        {
            var pageX = bounds.Left + x - sheetLeft;
            graphics.DrawLine(pen, pageX, bounds.Top, pageX, bounds.Bottom);
        }
        for (; y <= sheetTop + bounds.Height; y += spacing)
        {
            var pageY = bounds.Top + y - sheetTop;
            graphics.DrawLine(pen, bounds.Left, pageY, bounds.Right, pageY);
        }
    }

    public static void DrawBadge(XGraphics graphics, PdfLabelText text, SheetWindow sheet, XRect bounds, double scale = 1)
    {
        var label = ColumnName(sheet.Column) + (sheet.Row + 1);
        var size = 18 * scale;
        var height = 24 * scale;
        var textWidth = PdfLabelText.Measure(label, size, bold: true);
        var width = Math.Max(36 * scale, textWidth + 20 * scale);
        var x = sheet.Column == 0 ? bounds.Left + scale : bounds.Right - width - scale;
        var y = sheet.Row == 0 ? bounds.Top + scale : bounds.Bottom - height - scale;
        graphics.DrawRoundedRectangle(new XPen(XColors.Black, 1.625 * scale), XBrushes.White,
            new XRect(x, y, width, height), new XSize(12 * scale, 12 * scale));
        text.Draw(graphics, label, size, true, XColors.Black,
            x + (width - textWidth) / 2, y + height - 5.6 * scale);
    }

    public static void DrawCoordinates(XGraphics graphics, PdfLabelText text, GridTemplate grid, SheetPlan plan, SheetWindow sheet)
        => DrawCoordinates(graphics, text, grid, plan, sheet.Left, sheet.Top,
            new XRect(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight));

    public static void DrawOverviewCoordinates(XGraphics graphics, PdfLabelText text, GridTemplate grid, SheetPlan plan,
        double left, double top, double scale)
        => DrawCoordinates(graphics, text, grid, plan, 0, 0, new XRect(0, 0, plan.CanvasWidth, plan.CanvasHeight), left, top, scale);

    private static void DrawCoordinates(XGraphics graphics, PdfLabelText text, GridTemplate grid, SheetPlan plan,
        double sheetLeft, double sheetTop, XRect bounds, double originX = 0, double originY = 0, double scale = 1)
    {
        var spacing = plan.GridSpacing;
        var bar = spacing / 2;
        var left = bounds.Left + plan.MapLeft - sheetLeft;
        var top = bounds.Top + plan.MapTop - sheetTop;
        var right = left + plan.MapWidth;
        var bottom = top + plan.MapHeight;

        var column = 0;
        for (var x = FirstLine(left + plan.MapWidth / 2 + grid.OffsetX % 1 * spacing, left, spacing);
             x + spacing <= right + 1e-9; x += spacing, column++)
        {
            if (!Inside(x + bar, bounds.Left, bounds.Right))
                continue;
            Cell(ColumnName(column), column, x, top - bar, spacing, bar);
            Cell(ColumnName(column), column, x, bottom, spacing, bar);
        }

        var row = 0;
        for (var y = FirstLine(top + plan.MapHeight / 2 + grid.OffsetY % 1 * spacing, top, spacing);
             y + spacing <= bottom + 1e-9; y += spacing, row++)
        {
            if (!Inside(y + bar, bounds.Top, bounds.Bottom))
                continue;
            if (Inside(left, bounds.Left, bounds.Right))
                Cell((row + 1).ToString(), row, left - bar, y, bar, spacing, vertical: true);
            if (Inside(right, bounds.Left, bounds.Right))
                Cell((row + 1).ToString(), row, right, y, bar, spacing, vertical: true);
        }

        void Cell(string label, int index, double x, double y, double width, double height, bool vertical = false) =>
            DrawCell(graphics, text, label, index, originX + x * scale, originY + y * scale,
                width * scale, height * scale, scale, vertical);
    }

    private static void DrawCell(XGraphics graphics, PdfLabelText text, string label, int index,
        double x, double y, double width, double height, double scale, bool vertical)
    {
        var background = (index + 1) % 6 == 0 ? BorderDark : BorderLight;
        var border = scale == 1 ? BorderPen : new XPen(BorderPen.Color, 0.5 * scale);
        graphics.DrawRectangle(border, background, x, y, width, height);
        var distance = ((index + 1) * 5).ToString();
        var bar = vertical ? width : height;
        var labelSize = bar * 0.95;
        var distanceSize = bar * 0.85;
        const double labelScale = 0.60;
        const double distanceScale = 0.50;
        var distanceWidth = PdfLabelText.Measure(distance, distanceSize, bold: true) * distanceScale;

        if (vertical)
        {
            var baselineX = x + bar * 1.3 - labelSize / 2;
            text.Draw(graphics, label, labelSize, true, CoordinateInk, baselineX,
                y + height * 0.9, rotated: true, widthScale: labelScale);
            text.Draw(graphics, distance, distanceSize, true, DistanceInk, baselineX,
                y + height * 0.1 + distanceWidth, rotated: true, widthScale: distanceScale);
        }
        else
        {
            var inset = bar * 0.18;
            text.Draw(graphics, label, labelSize, true, CoordinateInk, x + inset,
                Baseline(labelSize), widthScale: labelScale);
            text.Draw(graphics, distance, distanceSize, true, DistanceInk, x + width - inset - distanceWidth,
                Baseline(distanceSize), widthScale: distanceScale);
        }

        // pdf-lib includes descenders by default, so both height calls in
        // TypeScript's centeredBaseline return the same value.
        double Baseline(double size) =>
            y + bar - ((bar - PdfLabelText.Height(size, bold: true)) / 2 + bar * 0.07);
    }

    private static bool Inside(double value, double min, double max) => value >= min && value <= max;

    private static double FirstLine(double origin, double start, double spacing) =>
        origin + Math.Ceiling((start - origin) / spacing - 1e-9) * spacing;

    private static string ColumnName(int index)
    {
        var name = "";
        for (var value = index + 1; value > 0; value = (value - 1) / 26)
            name = (char)('A' + (value - 1) % 26) + name;
        return name;
    }
}
