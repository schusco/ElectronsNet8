using Electrons.Core.Net8.Entities;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using System;
using System.IO;
using System.Linq;

namespace Electrons.Core.Net8.Infrastructure
{
    public static class PdfGenerator
    {
        public static byte[] Schedule(Repository repo, int year)
        {
            Document doc;
            var ms = new MemoryStream();


            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            doc = new Document(pdf, PageSize.LETTER);

            var fonts = new FontSet();
            fonts.AddDirectory("C:\\WINDOWS\\Fonts");
            var provider = new FontProvider(fonts);
            provider.AddStandardPdfFonts();
            provider.AddSystemFonts();
            doc.SetFontProvider(provider);

            var table = new Table(4)
                .SetHorizontalAlignment(HorizontalAlignment.LEFT)
                .UseAllAvailableWidth()
                .SetMarginTop(StandardSpacing);

            table.AddCell(CreateHeaderCell(string.Format("Electrons Schedule {0}", year), ColorConstants.LIGHT_GRAY, HeaderFontSize, 4));
            table.AddCell(CreateCell("Date"));
            table.AddCell(CreateCell("Opponent"));
            table.AddCell(CreateCell("Location"));
            table.AddCell(CreateCell("Result"));
            var games = repo.GetGamesByYear(year).OrderBy(o => o.GameDate);
            foreach (var game in games)
            {
                table.AddCell(CreateCell(game.GameDate.ToString("M/d/yyyy h:m tt")));
                table.AddCell(CreateCell(string.Format("{0} {1}", game.HV.ToString() == "V" ? "@ " : "vs. ", game.Opponent)));
                table.AddCell(CreateCell(game.Location.Field));
                table.AddCell(CreateCell(game.GameDate < DateTime.Now ? GameData.GetScore(game.IsHome, game.Innings.Sum(s => s.HomeRuns) ?? 0, game.Innings.Sum(s => s.AwayRuns) ?? 0) : ""));
            }
            doc.Add(table);
            doc.Close();
            var bytes = ms.ToArray();
            ms.Dispose();
            return bytes;

        }

        private static Cell CreateHeaderCell(string celltext, Color color = null, int fontSize = CellFontSize, int colSpan = 2)
        {
            if (color == null)
                color = ColorConstants.WHITE;
            var cell = new Cell(0, colSpan).SetMinHeight(13)
                .SetPaddingTop(0f)
                .SetPaddingBottom(3f)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            cell.Add(CreateParagraph(celltext, StandardFont, fontSize));
            cell.SetBackgroundColor(color);
            return cell;
        }

        private static Cell CreateCell(string celltext)
        {
            var cell = new Cell()
                .SetPaddingTop(0f)
                .SetPaddingBottom(3f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            IBlockElement contents;
            if (!string.IsNullOrEmpty(celltext) && celltext.Contains("<ul>"))
            {
                var array = celltext.Split('<');
                var list = new List();
                list.SetListSymbol("\u2022");
                list.SetSymbolIndent(10f);
                foreach (var s in array.Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    var text = s.Remove(0, s.IndexOf('>') + 1);
                    if (!string.IsNullOrEmpty(text))
                    {
                        var bullet = new ListItem(text);
                        bullet.SetFont(StandardFont);
                        list.Add(bullet);
                    }
                }
                contents = list;
            }
            else
                contents = CreateParagraph(celltext, StandardFont, CellFontSize);
            cell.Add(contents);
            return cell;
        }

        private static Paragraph CreateParagraph(string content, PdfFont font, float fontSize)
        {
            var pg = new Paragraph(content)
                .SetFont(font)
                .SetFontSize(fontSize);

            return pg;
        }

        private static PdfFont StandardFont
        {
            get
            {
                return PdfFontFactory.CreateFont(FontName, PdfEncodings.IDENTITY_H);
            }
        }

        private const int CellFontSize = 9;
        private const int HeaderFontSize = 11;
        private const float StandardSpacing = 5;
        private const string FontName = @"C:\Windows\Fonts\Calibri.ttf";
    }
}
