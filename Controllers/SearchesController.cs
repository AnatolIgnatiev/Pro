using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro.Models;
using Microsoft.AspNetCore.Authorization;

namespace Pro.Controllers
{
    [Authorize(Policy = "Admin")]
    public class SearchesController : Controller
    {
        public IActionResult Searches()
        {
            using (ProDBContext db = new ProDBContext())
            {
                ViewBag.searches = db.Searches.OrderBy(s => s.Date).ToList();
                return View();
            }
        }
        public List<Result> GetSearchResults(string searchId)
        {
            using (ProDBContext db = new ProDBContext())
            {
                var results = db.Results.Where(r => r.SearchId == searchId).ToList();
                return results;
            }
        }
    }
}
