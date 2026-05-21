using Microsoft.AspNetCore.Mvc;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public HomeController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    } 
}
