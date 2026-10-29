using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VongQuayMayManNQC.Models;

namespace VongQuayMayManNQC.Helpers
{
    public static class ExcelHelper
    {
        static ExcelHelper()
        {
            ExcelPackage.License.SetNonCommercialPersonal("NQC");
        }

        public static List<string> ReadNamesFromExcel(string filePath)
        {
            List<string> result = new List<string>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null)
                    return result;

                int rows = ws.Dimension.End.Row;
                int cols = ws.Dimension.End.Column;

                for (int r = 1; r <= rows; r++)
                {
                    for (int c = 1; c <= cols; c++)
                    {
                        string text = ws.Cells[r, c].Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            text = text.Trim();
                            if (!result.Any(x => x.Equals(text, StringComparison.OrdinalIgnoreCase)))
                                result.Add(text);
                        }
                    }
                }
            }

            return result;
        }

        public static void ExportWinners(string filePath, List<WinnerRecord> winners)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Winners");

                ws.Cells[1, 1].Value = "STT";
                ws.Cells[1, 2].Value = "Người trúng";
                ws.Cells[1, 3].Value = "Giải";
                ws.Cells[1, 4].Value = "Thời gian";

                using (ExcelRange range = ws.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                for (int i = 0; i < winners.Count; i++)
                {
                    WinnerRecord w = winners[i];
                    int row = i + 2;
                    ws.Cells[row, 1].Value = w.STT;
                    ws.Cells[row, 2].Value = w.Name;
                    ws.Cells[row, 3].Value = w.PrizeName;
                    ws.Cells[row, 4].Value = w.WinTime.ToString("dd/MM/yyyy HH:mm:ss");
                }

                ws.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }
    }
}