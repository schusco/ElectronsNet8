using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Electrons.Core.Net8.Infrastructure
{
    public class ExcelGenerator
    {
        static ExcelGenerator()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Scott Schuster");
        }
        public static Stream Export(int year, Repository repo)
        {
            var ms = new MemoryStream();
            var hitting = repo.GetSeasonHittingStats(year);
            var pitching = repo.GetSeasonPitchingStats(year);
            var row = 2;
            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add(DateTime.Now.Year.ToString());
                var cell = ws.Cells[1, 1];
                cell.Value = "Pitching Stats";
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 14;
                Output(pitching, ws, ref row);
                row++;
                cell = ws.Cells[row, 1];
                cell.Value = "Hitting Stats";
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 14;
                row++;
                Output(hitting, ws, ref row);
                pkg.SaveAs(ms);
                ms.Position = 0;
                return ms;
            }
        }

        private static void Output<T>(IList<T> stats, ExcelWorksheet ws, ref int row)
        {
            int col = 1;
            var pitchType = stats.First().GetType();
            var props = pitchType.GetProperties().Where(w => w.GetCustomAttributes(false).Any(a => a is TableColumnAttribute));
            foreach (var prop in props.Where(w => w.IncludeColumn(stats)))
            {
                var font = ws.Cells[row, col].Style.Font;
                font.Bold = true;
                font.UnderLine = true;
                if (col != 1)
                    ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, col].Value = ((TableColumnAttribute)prop.GetCustomAttributes(false).Single(s => s is TableColumnAttribute)).HeaderText;
                col++;
            }
            col = 1;
            row++;
            foreach (var stat in stats)
            {
                foreach (var prop in props.Where(w => w.IncludeColumn(stats)))
                {
                    if (col != 1)
                        ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    if (prop.PropertyType == typeof(decimal?))
                    {
                        if (typeof(T) == typeof(HittingStatsRow))
                            ws.Cells[row, col].Style.Numberformat.Format = "#0.000";
                        else
                            ws.Cells[row, col].Style.Numberformat.Format = "#0.00";
                    }
                    ws.Cells[row, col].Value = prop.GetValue(stat);
                    col++;
                }
                row++;
                col = 1;
            }
            ws.Column(1).AutoFit();
        }
    }
}
