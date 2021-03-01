using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pro.Controllers
{
    public class SearchController
    {
        
        public static List<ReadFileResult> GetSearchRequests(List<IFormFile> fileNames)
        {
            var result = new List<ReadFileResult>();
            foreach (var fileName in fileNames)
            {
                var fileResult = new ReadFileResult { FileName = fileName, Requests = new List<SearchRequest>() };

                try
                {
                    using (var document = SpreadsheetDocument.Open(fileName.OpenReadStream(), false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var data = workbookPart.WorksheetParts.First().Worksheet.Elements<SheetData>().First();
                        foreach (var row in data.Elements<Row>().Skip(1))
                        {
                            var cells = row.Elements<Cell>();
                            fileResult.Requests.Add(new SearchRequest
                            {
                                Id = GetCellValue(cells.ElementAt(0), workbookPart),
                                Brand = GetCellValue(cells.ElementAt(1), workbookPart),
                                OriginalPrice = GetCellValue(cells.ElementAt(4), workbookPart)
                            });
                        }
                        fileResult.Success = true;
                    }
                }
                catch
                {
                    fileResult.Success = false;
                }
                result.Add(fileResult);
            }
            return result;
        }
        private static string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            string cellValue = null;
            if (cell.DataType != null && cell.DataType == CellValues.SharedString)
            {
                var stringId = Convert.ToInt32(cell.InnerText);
                cellValue = workbookPart.SharedStringTablePart.SharedStringTable
                    .Elements<SharedStringItem>().ElementAt(stringId).InnerText;
            }
            else
            {
                cellValue = cell.InnerText;
            }
            return cellValue;
        }
    }
}
