using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Dmsh.Core;

public static class MapPdfExporter
{
    public static void Export(MapTemplate template, string imagePath, Stream destination)
    {
        using var jpeg = WorkingImage.CreateJpeg(imagePath);
        using var image = XImage.FromStream(jpeg);
        var plan = SheetPlan.Create(template, image.PixelWidth, image.PixelHeight);
        using var document = new PdfDocument();
        // PDFsharp 6.2.4 emitted Flate streams without a complete zlib
        // trailer in an Acrobat-rejected export.
        document.Options.CompressContentStreams = false;
        document.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
        document.Info.Title = template.Title;

        if (plan.HasOverview)
        {
            using var graphics = AddPage(document, plan);
            var scale = Math.Min(plan.PrintableWidth / plan.CanvasWidth, plan.PrintableHeight / plan.CanvasHeight);
            var left = (plan.PageWidth - plan.CanvasWidth * scale) / 2;
            var top = (plan.PageHeight - plan.CanvasHeight * scale) / 2;
            graphics.DrawImage(image, left + plan.MapLeft * scale, top + plan.MapTop * scale,
                plan.MapWidth * scale, plan.MapHeight * scale);

            var overviewState = graphics.Save();
            graphics.TranslateTransform(left, top);
            graphics.ScaleTransform(scale, scale);
            var gridState = graphics.Save();
            var canvasClip = new XGraphicsPath();
            canvasClip.AddRectangle(0, 0, plan.CanvasWidth, plan.CanvasHeight);
            graphics.IntersectClip(canvasClip);
            SheetArtwork.DrawOverviewGrid(graphics, template.Grid, plan);
            graphics.Restore(gridState);

            var outline = new XPen(XColors.Gray, 1 / scale);
            foreach (var sheet in plan.Sheets)
                graphics.DrawRectangle(outline, sheet.Left, sheet.Top, plan.PrintableWidth, plan.PrintableHeight);
            if (plan.HasCoordinateBorder)
                SheetArtwork.DrawOverviewCoordinates(graphics, template.Grid, plan);
            foreach (var sheet in plan.Sheets)
                SheetArtwork.DrawBadge(graphics, sheet,
                    new XRect(sheet.Left, sheet.Top, plan.PrintableWidth, plan.PrintableHeight));
            graphics.Restore(overviewState);
        }

        foreach (var sheet in plan.Sheets)
        {
            using var graphics = AddPage(document, plan);
            var clip = new XGraphicsPath();
            var state = graphics.Save();
            clip.AddRectangle(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight);
            graphics.IntersectClip(clip);
            graphics.DrawImage(image, plan.Margin + plan.MapLeft - sheet.Left,
                plan.Margin + plan.MapTop - sheet.Top, plan.MapWidth, plan.MapHeight);
            SheetArtwork.DrawGrid(graphics, template.Grid, plan, sheet);
            graphics.Restore(state);
            if (plan.HasCoordinateBorder)
                SheetArtwork.DrawCoordinates(graphics, template.Grid, plan, sheet);
            SheetArtwork.DrawBadge(graphics, sheet,
                new XRect(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight));
        }

        document.Save(destination, false);
    }

    private static XGraphics AddPage(PdfDocument document, SheetPlan plan)
    {
        var page = document.AddPage();
        page.Width = XUnit.FromPoint(plan.PageWidth);
        page.Height = XUnit.FromPoint(plan.PageHeight);
        return XGraphics.FromPdfPage(page);
    }
}
