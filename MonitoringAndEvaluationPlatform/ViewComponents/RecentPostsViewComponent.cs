using Microsoft.AspNetCore.Mvc;

namespace MonitoringAndEvaluationPlatform.ViewComponents
{
    public class RecentPostsViewComponent : ViewComponent
    {
        private readonly IPostService _postService;

        public RecentPostsViewComponent(IPostService postService)
        {
            _postService = postService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count)
        {
            var posts = await _postService.GetRecentPostsAsync(count);
            return View(posts); // Looks for Views/Shared/Components/RecentPosts/Default.cshtml
        }
    }

}
