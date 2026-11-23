using PdfSharp.Drawing;
using SkiaSharp;

namespace Dmsh.Core;

internal static class SheetArtwork
{
    private static readonly XSolidBrush BorderLight = new(XColor.FromArgb(240, 235, 219));
    private static readonly XSolidBrush BorderDark = new(XColor.FromArgb(186, 177, 151));
    private static readonly XPen BorderPen = new(XColor.FromArgb(89, 74, 46), 0.5);

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

    public static void DrawBadge(XGraphics graphics, SheetWindow sheet, XRect bounds)
    {
        var label = ColumnName(sheet.Column) + (sheet.Row + 1);
        const double fontSize = 18;
        const double height = 24;
        const double baselineFromBottom = 5.6;
        const float rasterScale = 4;
        using var typeface = SKTypeface.FromFamilyName(
            OperatingSystem.IsMacOS() ? "Helvetica" : "Arial", SKFontStyle.Bold);
        using var font = new SKFont(typeface, (float)fontSize * rasterScale);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        var textWidth = font.MeasureText(label, out _, paint) / rasterScale;
        var width = Math.Max(36, textWidth + 20);
        var x = sheet.Column == 0 ? bounds.Left + 1 : bounds.Right - width - 1;
        var y = sheet.Row == 0 ? bounds.Top + 1 : bounds.Bottom - height - 1;
        graphics.DrawRoundedRectangle(new XPen(XColors.Black, 1.625), XBrushes.White,
            new XRect(x, y, width, height), new XSize(12, 12));
        // Keep the opaque text image inside the rounded badge and position
        // the baseline from the bottom edge, as the TypeScript renderer does.
        const double imageInsetX = 6;
        const double imageInsetY = 2;
        var imageWidth = width - 2 * imageInsetX;
        var imageHeight = height - 2 * imageInsetY;
        using var bitmap = new SKBitmap((int)Math.Ceiling(imageWidth * rasterScale),
            (int)Math.Ceiling(imageHeight * rasterScale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        canvas.Scale((float)(bitmap.Width / imageWidth), (float)(bitmap.Height / imageHeight));
        font.Size = (float)fontSize;
        canvas.DrawText(label, new SKPoint((float)((width - textWidth) / 2 - imageInsetX),
                (float)(height - baselineFromBottom - imageInsetY)), SKTextAlign.Left, font, paint);
        DrawLabelImage(graphics, bitmap, x + imageInsetX, y + imageInsetY, imageWidth, imageHeight);
    }

    public static void DrawCoordinates(XGraphics graphics, GridTemplate grid, SheetPlan plan, SheetWindow sheet)
        => DrawCoordinates(graphics, grid, plan, sheet.Left, sheet.Top,
            new XRect(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight));

    public static void DrawOverviewCoordinates(XGraphics graphics, GridTemplate grid, SheetPlan plan)
        => DrawCoordinates(graphics, grid, plan, 0, 0, new XRect(0, 0, plan.CanvasWidth, plan.CanvasHeight));

    private static void DrawCoordinates(XGraphics graphics, GridTemplate grid, SheetPlan plan,
        double sheetLeft, double sheetTop, XRect bounds)
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
            DrawCell(graphics, ColumnName(column), column, x, top - bar, spacing, bar);
            DrawCell(graphics, ColumnName(column), column, x, bottom, spacing, bar);
        }

        var row = 0;
        for (var y = FirstLine(top + plan.MapHeight / 2 + grid.OffsetY % 1 * spacing, top, spacing);
             y + spacing <= bottom + 1e-9; y += spacing, row++)
        {
            if (!Inside(y + bar, bounds.Top, bounds.Bottom))
                continue;
            if (Inside(left, bounds.Left, bounds.Right))
                DrawCell(graphics, (row + 1).ToString(), row, left - bar, y, bar, spacing, vertical: true);
            if (Inside(right, bounds.Left, bounds.Right))
                DrawCell(graphics, (row + 1).ToString(), row, right, y, bar, spacing, vertical: true);
        }
    }

    private static void DrawCell(XGraphics graphics, string label, int index,
        double x, double y, double width, double height, bool vertical = false)
    {
        var background = (index + 1) % 6 == 0 ? BorderDark : BorderLight;
        graphics.DrawRectangle(BorderPen, background, x, y, width, height);
        var distance = ((index + 1) * 5).ToString();
        var size = Math.Min(width, height) * 0.85;
        if (vertical)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(x, y + height);
            graphics.RotateTransform(-90);
            DrawText(graphics, label, 0, 0, height / 2, width, size, background.Color);
            DrawText(graphics, distance, height / 2, 0, height / 2, width, size, background.Color, isDistance: true);
            graphics.Restore(state);
        }
        else
        {
            DrawText(graphics, label, x, y, width / 2, height, size, background.Color);
            DrawText(graphics, distance, x + width / 2, y, width / 2, height, size, background.Color, isDistance: true);
        }
        graphics.DrawRectangle(BorderPen, x, y, width, height);
    }

    // Rasterize short labels at print resolution so PDFs do not depend on a
    // PDFsharp font resolver or redistributing a platform font.
    private static void DrawText(XGraphics graphics, string text,
        double x, double y, double width, double height, double fontSize, XColor background,
        bool isDistance = false)
    {
        const float scale = 4;
        using var bitmap = new SKBitmap((int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(background.R, background.G, background.B));
        using var typeface = SKTypeface.FromFamilyName("sans-serif",
            isDistance ? SKFontStyle.Normal : SKFontStyle.Bold);
        using var font = new SKFont(typeface, (float)fontSize * scale);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        if (isDistance)
        {
            // Use the same narrow glyphs regardless of the number of digits.
            font.ScaleX = 0.5f;
        }
        else
        {
            var measured = font.MeasureText(text, out _, paint);
            if (measured > bitmap.Width * 0.9f)
                font.ScaleX = bitmap.Width * 0.9f / measured;
        }
        font.MeasureText(text, out var bounds, paint);
        canvas.DrawText(text, new SKPoint(bitmap.Width / 2f - bounds.MidX, bitmap.Height / 2f - bounds.MidY),
            SKTextAlign.Left, font, paint);
        DrawLabelImage(graphics, bitmap, x, y, width, height);
    }

    private static void DrawLabelImage(XGraphics graphics, SKBitmap bitmap,
        double x, double y, double width, double height)
    {
        using var jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);
        using var stream = new MemoryStream(jpeg.ToArray(), writable: false);
        using var image = XImage.FromStream(stream);
        graphics.DrawImage(image, x, y, width, height);
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
