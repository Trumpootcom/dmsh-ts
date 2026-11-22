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
        document.Info.Title = template.Title;

        if (plan.HasOverview)
        {
            using var graphics = AddPage(document, plan);
            var scale = Math.Min(plan.PrintableWidth / plan.CanvasWidth, plan.PrintableHeight / plan.CanvasHeight);
            var left = (plan.PageWidth - plan.CanvasWidth * scale) / 2;
            var top = (plan.PageHeight - plan.CanvasHeight * scale) / 2;
            graphics.DrawImage(image, left + plan.MapLeft * scale, top + plan.MapTop * scale,
                plan.MapWidth * scale, plan.MapHeight * scale);
            foreach (var sheet in plan.Sheets)
                graphics.DrawRectangle(XPens.Gray, left + sheet.Left * scale, top + sheet.Top * scale,
                    plan.PrintableWidth * scale, plan.PrintableHeight * scale);
        }

        foreach (var sheet in plan.Sheets)
        {
            using var graphics = AddPage(document, plan);
            var clip = new XGraphicsPath();
            clip.AddRectangle(plan.Margin, plan.Margin, plan.PrintableWidth, plan.PrintableHeight);
            graphics.IntersectClip(clip);
            graphics.DrawImage(image, plan.Margin + plan.MapLeft - sheet.Left,
                plan.Margin + plan.MapTop - sheet.Top, plan.MapWidth, plan.MapHeight);
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
