using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface IPostService
{
    Task<List<Post>> GetRecentPostsAsync(int count);
}