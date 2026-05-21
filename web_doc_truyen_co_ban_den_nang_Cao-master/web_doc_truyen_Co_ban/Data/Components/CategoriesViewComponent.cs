using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace web_doc_truyen_Co_ban.Data.Components
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _dataContext;

        public CategoriesViewComponent(ApplicationDbContext context)
        {
            _dataContext = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            string viewName = "Default")
        {
            var categories = await _dataContext.Categories.ToListAsync();

            return View(viewName, categories);
        }
    }
}