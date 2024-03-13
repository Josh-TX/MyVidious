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

        public TestController(IHttpClientFactory httpClientFactory, VideoDbContext videoDbContext, InvidiousAPIAccess invidiousAPIAccess)
        {
            _httpClient = httpClientFactory.CreateClient();
            _videoDbContext = videoDbContext;
            _invidiousAPIAccess = invidiousAPIAccess;
        }

        [Route("add-channel/{channelName}")]
        [HttpGet]
        public async Task<IActionResult> AddChannel([FromRoute] string channelName)
        {
            var searchResults = await _invidiousAPIAccess.Search(new SearchRequest
            {
                Q = channelName,
                Type = "channel"
            });
            var channel = searchResults.OfType<SearchResponse_Channel>().FirstOrDefault(z => z.ChannelHandle.ToLower().Trim('@') == channelName.ToLower().Trim('@'));
            if (channel == null)
            {
                return NotFound();
            }
            var existingDb = _videoDbContext.Channels.FirstOrDefault(z => z.UniqueId == channel.AuthorId);
            if (existingDb == null) {
                _videoDbContext.Channels.Add(new ChannelEntity
                {
                    Name = channel.Author,
                    UniqueId = channel.AuthorId,
                    Handle = channel.ChannelHandle,
                    DateLastScraped = null,
                    ScrapedToOldest = false,
                    ScrapeFailureCount = 0,
                });
                _videoDbContext.SaveChanges();
            }
            return Ok();
        }

        [Route("")]
        [HttpPost]
        public async Task<IActionResult> PostData()
        {
            MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "aSampleMasterKey");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string jsonString = await System.IO.File.ReadAllTextAsync("movies.json");
            var movies = JsonSerializer.Deserialize<IEnumerable<Movie>>(jsonString, options);

            var index = client.Index("movies");
            await index.UpdateSearchableAttributesAsync(new[] { "title", "overview" });
            await index.AddDocumentsAsync<Movie>(movies);     
            return Ok();      
        }

        [Route("seed")]
        [HttpPost]
        public async Task<IActionResult> SeedExistingVideos()
        {
            var videoEntities = _videoDbContext.Videos.Include(z => z.Channel).ToList();
            var meilisearchVideos = videoEntities.Select(z => new VideoMeilisearch
            {
                Id = z.Id,
                Title = z.Title,
                Description = z.Description.Substring(0, Math.Min(150, z.Description.Length)),
                ChannelHandle = z.Channel.Handle,
                ChannelName = z.Channel.Name,
            });
            MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "aSampleMasterKey");
            var index = client.Index("videos");
            await index.UpdateSearchableAttributesAsync(new[] { "title", "channelname", "channelhandle", "description" });
            await index.AddDocumentsAsync(meilisearchVideos);
            return Ok();
        }

        [Route("")]
        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "masterKey");
            var index = client.Index("movies");

            var movies = await index.SearchAsync<Movie>("botman");
            foreach (var movie in movies.Hits)
            {
                Console.WriteLine(movie.Title);
            }
            return Ok(movies.Hits);
        }
    }
    
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Poster { get; set; }
        public string Overview { get; set; }
        public IEnumerable<string> Genres { get; set; }
    }
}