using Microsoft.EntityFrameworkCore;

namespace MyVidious.Data;

public class VideoDbContext : DbContext
{
    public VideoDbContext(DbContextOptions<VideoDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlgorithmVideoEntity>().HasNoKey();
        modelBuilder.Entity<ChannelVideoCountEntity>().HasNoKey();
        modelBuilder.Entity<AlgorithmItemInfoEntity>().HasNoKey();
    }

    public DbSet<ChannelEntity> Channels { get; set; }
    public DbSet<VideoEntity> Videos { get; set; }
    public DbSet<AlgorithmEntity> Algorithms { get; set; }


    //VIEWS
    private DbSet<ChannelVideoCountEntity> _channelVideoCounts { get; set; }
    public IQueryable<ChannelVideoCountEntity> ChannelVideoCounts { get => _channelVideoCounts.AsQueryable(); }
    private DbSet<AlgorithmItemInfoEntity> _algorithmItemInfos { get; set; }
    public IQueryable<AlgorithmItemInfoEntity> AlgorithmItemInfos { get => _algorithmItemInfos.AsQueryable(); }

    //PROCS
    private DbSet<AlgorithmVideoEntity> _algorithmVideos { get; set; }
    public List<AlgorithmVideoEntity> GetAlgorithmVideos(int algorithmId, int take)
    {
        var results = _algorithmVideos.FromSqlRaw("EXEC GetAlgorithmVideos " + algorithmId + ", " + take).ToList();
        return results;
    }

}