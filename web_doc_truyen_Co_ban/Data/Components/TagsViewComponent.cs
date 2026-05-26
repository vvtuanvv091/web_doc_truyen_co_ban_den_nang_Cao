using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ← đổi cái này

namespace web_doc_truyen_Co_ban.Data.Components
{
    public class TagsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _dbcontext;

        public TagsViewComponent(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
            => View(await _dbcontext.Tags.ToListAsync());
    }
}