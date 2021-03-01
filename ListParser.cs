using HtmlAgilityPack;
using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pro
{
    public class ListParser
    {
        private readonly HttpClient client;
        private readonly Dictionary<string, ListParseResult> cache;
        private object cahceLock = new object();

        public ListParser()
        {
            cache = new Dictionary<string, ListParseResult>();
            client = new HttpClient();
            client.BaseAddress = new Uri("https://avto.pro");
        }

        public List<TableRow> ParseList(string uri)
        {
            ListParseResult parseResult;
            lock (cahceLock)
            {
                var key = GetKey(uri);
                if (!cache.TryGetValue(key, out parseResult))
                {
                    parseResult = GetParseResult(uri);
                    cache.Add(key, parseResult);
                }
                else if (parseResult.Rows.Status != TaskStatus.Running && parseResult.DueDate < DateTime.Now)
                {
                    parseResult = GetParseResult(uri);
                    cache[key] = parseResult;
                }
                else
                { 
                }
            }
            return parseResult.Rows.Result;
        }

        private string GetKey(string uri)
        {
            var match = Regex.Match(uri, "&uri.*?&");
            return match.Success ? match.Value : uri;
        }

        private ListParseResult GetParseResult(string uri)
        {
            return new ListParseResult
            {
                DueDate = DateTime.Now.AddMinutes(2),
                Rows = Task.Run(() =>
                {
                    var matchedRows = new List<TableRow>();
                    var request = new HttpRequestMessage(HttpMethod.Get, uri.StartsWith("/") ? uri.Substring(1) : uri);
                    var result = client.SendAsync(request).Result;
                    if (result.StatusCode == HttpStatusCode.Moved)
                    {
                        result = client.SendAsync(request).Result;
                    }
                    if (result.StatusCode == HttpStatusCode.Found)
                    {
                        var match = Regex.Match(result.Content.ReadAsStringAsync().Result, "<a href=\"\\/ (.*)\\/ \">");
                        if (match.Success)
                        {
                            var partUri = match.Groups[1].Value;
                            result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, partUri)).Result;
                        }
                    }
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var doc = new HtmlDocument();
                        var html = result.Content.ReadAsStringAsync().Result;
                        doc.LoadHtml(html);
                        var tmp = doc.DocumentNode
                            .SelectNodes("//table[@id='js-partslist-primary']//tr");
                        var rows = tmp
                            .Cast<HtmlNode>()
                            .Select(row => new
                            {
                                Row = row,
                                Cells = row.SelectNodes("td")
                                            ?.Cast<HtmlNode>()
                                            .Select(cell => new
                                            {
                                                CellType = cell.GetAttributeValue("data-type", string.Empty),
                                                Text = Regex.Replace(cell.InnerText.Trim(), "(\\r\\n)+|\\r+|\\n+|\\t+", string.Empty)
                                            })
                            });
                        foreach (var row in rows)
                        {
                            if (row.Cells != null)
                            {
                                var matchedRow = new TableRow();
                                foreach (var cell in row.Cells)
                                {
                                    switch (cell.CellType)
                                    {
                                        case "maker":
                                            matchedRow.Brand = cell.Text;
                                            break;
                                        case "code":
                                            matchedRow.Id = cell.Text;
                                            break;
                                        case "delivery":
                                            matchedRow.DeliveryText = cell.Text;
                                            break;
                                        case "price":
                                            matchedRow.Price = cell.Text;
                                            break;
                                    }
                                }
                                matchedRows.Add(matchedRow);
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                    return matchedRows;
                })
            };
        }
    }
}
