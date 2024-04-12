using Microsoft.AspNetCore.Mvc;

using MyVidious.Access;
using MyVidious.Data;
using MyVidious.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace MyVidious.Controllers
{
    [ApiController]
    public class HomeController : Controller
    {
        private readonly VideoDbContext _videoDbContext;
        private readonly InvidiousAPIAccess _invidiousAPIAccess;
        private readonly AlgorithmAccess _algorithmAccess;

        public HomeController(
            InvidiousAPIAccess invidiousAPIAccess, 
            VideoDbContext videoDbContext,
            AlgorithmAccess algorithmAccess)
        {
            _videoDbContext = videoDbContext;
            _invidiousAPIAccess = invidiousAPIAccess;
            _algorithmAccess = algorithmAccess;
        }
        [HttpGet("")]
        public async Task<IActionResult> GetRootPage()
        {
            var algorithms = await _videoDbContext.Algorithms.Where(z => z.IsListed).ToListAsync();
            var foundAlgorithms = algorithms.Select(z => new FoundAlgorithm
            {
                AlgorithmId = z.Id,
                AlgorithmName = z.Name.ToLower(),
                Description = z.Description,
                Username = z.Username.ToLower()
            }).ToList();
            return View("Root", foundAlgorithms);
        }
        
        private const int EST_VIDEO_COUNT = 100;


        [Route("{username}/{algorithmName}")]
        [HttpGet]
        public async Task<IActionResult> ViewAlgorithm([FromRoute] string username, [FromRoute] string algorithmName)
        {
            //since we cache the algorithmId, this will usually be more efficient
            var algorithmId = _algorithmAccess.GetAlgorithmId(username, algorithmName);
            if (!algorithmId.HasValue)
            {
                return View("NotFound");
            }
            var algorithmEntity = _videoDbContext.Algorithms.First(z => z.Id == algorithmId);
            var itemInfos = await _videoDbContext.GetAlgorithmItemInfos().Where(z => z.AlgorithmId == algorithmEntity.Id).ToListAsync();
            var sumWeight = itemInfos.Sum(z => Math.Min(z.MaxItemWeight, z.VideoCount) * Math.Max(z.WeightMultiplier, 0));
            var result = new LoadAlgorithmResult
            {
                AlgorithmId = algorithmEntity.Id,
                AlgorithmItems = itemInfos.Select(z => new LoadAlgorithmItem
                {
                    PlaylistId = z.PlaylistId,
                    ChannelId = z.ChannelId,
                    WeightMultiplier = z.WeightMultiplier,
                    Name = z.Name,
                    VideoCount = z.VideoCount,
                    UniqueId = z.UniqueId,
                    FailureCount = z.FailureCount,
                    EstimatedWeight = Math.Min(z.MaxItemWeight, z.VideoCount) * Math.Max(z.WeightMultiplier, 0)
                }),
                AlgorithmName = algorithmEntity.Name,
                MaxItemWeight = algorithmEntity.MaxItemWeight,
                Description = algorithmEntity.Description,
                Username = algorithmEntity.Username,
                EstimatedSumWeight = sumWeight,
            };
            return View("Algorithm", result);
        }


    }
}