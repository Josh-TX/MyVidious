using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Meilisearch;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyVidious.Data;
using MyVidious.Access;
using MyVidious.Models;
using Microsoft.EntityFrameworkCore;
using MyVidious.Models.Invidious;

namespace MyVidious.Controllers
{
    [Route("test")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TestController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly VideoDbContext _videoDbContext;
        private readonly InvidiousAPIAccess _invidiousAPIAccess;
        private readonly MeilisearchAccess _meilisearchAccess;

        public TestController(
            IHttpClientFactory httpClientFactory, 
            VideoDbContext videoDbContext, 
            InvidiousAPIAccess invidiousAPIAccess,
            MeilisearchAccess meilisearchAccess
            )
        {
            _httpClient = httpClientFactory.CreateClient();
            _videoDbContext = videoDbContext;
            _invidiousAPIAccess = invidiousAPIAccess;
            _meilisearchAccess = meilisearchAccess;
        }


        [Route("seed")]
        [HttpGet]
        public async Task<IActionResult> ReseedMeilisearch()
        {
            MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "masterKey");
            //await client.Index("videos").DeleteAsync();
            var videos = _videoDbContext.Videos.Include(z => z.Channel).ToList();
            await _meilisearchAccess.AddVideos(videos.Select(z => new VideoMeilisearch
            {
                ChannelId = z.ChannelId,
                Id = z.Id,
                ChannelName = z.Channel!.Name,
                Title = z.Title
            }));
            return Ok();
        }

        [Route("search")]
        [HttpGet]
        public async Task<IActionResult> SearchMeilisearch([FromQuery] string q, [FromQuery] int? id)
        {
            if (id == null)
            {
                id = 4;
            }
            MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "masterKey");
            //await client.Index("videos").DeleteAsync();
            var videos = _videoDbContext.Videos.Include(z => z.Channel).ToList();
            var res = await _meilisearchAccess.SearchVideoIds(q, 1, new[] {id.Value });
            return Ok(res);
        }
    }
}