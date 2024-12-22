using MonitoringAndEvaluationPlatform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PostService : IPostService
{
    private static readonly List<Post> Posts = new()
    {
        new Post { Id = 1, Title = "First Post", Content = "Content of the first post", DatePublished = DateTime.Now.AddDays(-5) },
        new Post { Id = 2, Title = "Second Post", Content = "Content of the second post", DatePublished = DateTime.Now.AddDays(-3) },
        new Post { Id = 3, Title = "Third Post", Content = "Content of the third post", DatePublished = DateTime.Now.AddDays(-1) },
        new Post { Id = 4, Title = "4 Post", Content = "Content of the 4 post", DatePublished = DateTime.Now.AddDays(-1) },
        new Post { Id = 5, Title = "5 Post", Content = "Content of the 5 post", DatePublished = DateTime.Now.AddDays(-1) },
        new Post { Id = 6, Title = "6 Post", Content = "Content of the 6 post", DatePublished = DateTime.Now.AddDays(-1) },
        new Post { Id = 7, Title = "7 Post", Content = "Content of the 7 post", DatePublished = DateTime.Now.AddDays(-1) }
    };

    public Task<List<Post>> GetRecentPostsAsync(int count)
    {
        var recentPosts = Posts.OrderByDescending(p => p.DatePublished).Take(count).ToList();
        return Task.FromResult(recentPosts);
    }
}
