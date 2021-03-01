using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pro.Hubs;
using Pro.Models;
using Pro.Rest.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pro.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string[] permittedExtensions = { ".xlsx" };
        private static SearchProcessor searchProcessor;
        private readonly IHubContext<ProHub> _hubContext;


        static HomeController()
        {
            searchProcessor = new SearchProcessor(new RestSearchController());
        }
        public HomeController(ILogger<HomeController> logger, IHubContext<ProHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;

        }
        private List<SearchRequest> GetFailedReqests(string searchId)
        {
            using (ProDBContext db = new ProDBContext())
            {
                return db.Results.Where(r => r.SearchId == searchId &&
                (r.IsSuccessful == false))
                    .Select(r => new SearchRequest
                    {
                        Id = r.PartId,
                        Brand = r.Brand,
                        OriginalPrice = r.OriginalPrice
                    }).ToList();
            };
        }
        private bool SaveSearchResults(SearchResult searchResult, string searchId, bool updateExistingResults)
        {
            bool isSucessful;
            using (ProDBContext db = new ProDBContext())
            {
                try
                {
                    if (updateExistingResults)
                    {
                        var searchToUpdate = db.Searches.Where(s => s.SearchId == searchId).SingleOrDefault();
                        searchToUpdate.Date = DateTime.Now;
                        var resultToUpdate = db.Results
                            .Where(r => r.SearchId == searchId &&
                                  r.PartId == searchResult.Id &&
                                  r.Brand == searchResult.Brand)
                            .SingleOrDefault();
                        resultToUpdate.FirstPrice = searchResult.FirstPrice;
                        resultToUpdate.SecondPrice = searchResult.SecondPrice;
                        resultToUpdate.IsSuccessful = searchResult.IsSuccessful;
                        db.SaveChanges();
                        isSucessful = true;
                    }
                    else
                    {
                        var result = new Result();
                        if (!db.Searches.Any(s => s.SearchId == searchId))
                        {
                            Search search = new Search { SearchId = searchId, Date = DateTime.Now };
                            db.Searches.AddAsync(search);
                            result = new Result
                            {
                                PartId = searchResult.Id,
                                Brand = searchResult.Brand,
                                OriginalPrice = searchResult.OriginalPrice,
                                FirstPrice = searchResult.FirstPrice,
                                SecondPrice = searchResult.SecondPrice,
                                SearchId = search.SearchId,
                                IsSuccessful = searchResult.IsSuccessful
                            };
                        }
                        else
                        {
                            result = new Result
                            {
                                PartId = searchResult.Id,
                                Brand = searchResult.Brand,
                                OriginalPrice = searchResult.OriginalPrice,
                                FirstPrice = searchResult.FirstPrice,
                                SecondPrice = searchResult.SecondPrice,
                                SearchId = searchId,
                                IsSuccessful = searchResult.IsSuccessful
                            };
                        }
                        
                        db.Results.AddAsync(result);
                        db.SaveChanges();
                        isSucessful = true;
                    }
                }
                catch (Exception e)
                {
                    isSucessful = false;
                }
            }
            return isSucessful;
        }
        public IActionResult Index()
        {
            return View();
        }

        public void CanceleSearch(string connectionId)
        {
            searchProcessor.CanceleSearch(connectionId);
        }

        public void SkipCurrentReqest(string connectionId)
        {
            searchProcessor.SkipCurrentReqest(connectionId);
        }
        public void Retry(string searchId, string connectionId)
        {
            try
            {
                searchProcessor.SearchReqestChanged += SearchProcessor_SearchReqestChanged;
                searchProcessor.CurrentSearchReqestCompleted += SearchProcessor_CurrentSearchReqestCompleted;
                searchProcessor.ProgressChagned += SearchProcessor_ProgressChagned;
                searchProcessor.Search(GetFailedReqests(searchId), searchId, true, connectionId);
            }
            finally
            {
                searchProcessor.SearchReqestChanged -= SearchProcessor_SearchReqestChanged;
                searchProcessor.CurrentSearchReqestCompleted -= SearchProcessor_CurrentSearchReqestCompleted;
                searchProcessor.ProgressChagned -= SearchProcessor_ProgressChagned;
            }
        }
        public string Search(List<IFormFile> uploadedFiles, string connectionId)
        {
            var searchId = Guid.NewGuid().ToString();
            try
            {
                searchProcessor.SearchReqestChanged += SearchProcessor_SearchReqestChanged;
                searchProcessor.ProgressChagned += SearchProcessor_ProgressChagned;
                searchProcessor.CurrentSearchReqestCompleted += SearchProcessor_CurrentSearchReqestCompleted;
                searchProcessor.Search(uploadedFiles, searchId, false, connectionId);
            }
            finally
            {
                searchProcessor.SearchReqestChanged -= SearchProcessor_SearchReqestChanged;
                searchProcessor.ProgressChagned -= SearchProcessor_ProgressChagned;
                searchProcessor.CurrentSearchReqestCompleted -= SearchProcessor_CurrentSearchReqestCompleted;
            }
            return searchId;
        }

        private void SearchProcessor_CurrentSearchReqestCompleted(SearchResult result, string searchId, bool updateExistingResults, string connectionId)
        {
            
            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveCurrentResult", result).Wait();
            SaveSearchResults(result, searchId, updateExistingResults);
        }

        private void SearchProcessor_ProgressChagned(int progress, string connectionId)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("ProgressChanged", progress).Wait();
        }


        private void SearchProcessor_SearchReqestChanged(string searchReqestMessage, string connectionId)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", searchReqestMessage).Wait();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
