using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Xml;

namespace MyVidious.Data;

public class VideoDbContext : DbContext
{
    public VideoDbContext(DbContextOptions<VideoDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlaylistVideoEntity>().HasKey(e => new { e.PlaylistId, e.VideoId });
        modelBuilder.Entity<AlgorithmVideoEntity>().HasNoKey();
        modelBuilder.Entity<AlgorithmItemInfoEntity>().HasNoKey();
        modelBuilder.Entity<VideoEntity>().HasIndex(z => z.UniqueId).IsUnique();
        modelBuilder.Entity<ChannelEntity>().HasIndex(z => z.UniqueId).IsUnique();
    }

    public DbSet<ChannelEntity> Channels { get; set; }
    public DbSet<PlaylistEntity> Playlists { get; set; }
    public DbSet<PlaylistVideoEntity> PlaylistVideos { get; set; }
    public DbSet<VideoEntity> Videos { get; set; }
    public DbSet<AlgorithmEntity> Algorithms { get; set; }
    public DbSet<AlgorithmItemEntity> AlgorithmItems { get; set; }


    public IQueryable<AlgorithmItemInfoEntity> GetAlgorithmItemInfos()
    {
        var sql = File.ReadAllText(Directory.GetCurrentDirectory() + "/Sql/AlgorithmItemInfo.sql");
        return this.Set<AlgorithmItemInfoEntity>()
            .FromSqlRaw(sql);
    }


    public List<AlgorithmVideoEntity> GetRandomAlgorithmVideos(int algorithmId, int take)
    {
        var sql = File.ReadAllText(Directory.GetCurrentDirectory() + "/Sql/RandomAlgorithmVideos.sql");
        var results = this.Set<AlgorithmVideoEntity>()
                       .FromSqlRaw(sql, algorithmId, take)
                       .ToList();
        return results;
    }
    public List<AlgorithmVideoEntity> GetRecentAlgorithmVideos(int algorithmId, int take)
    {
        var sql = File.ReadAllText(Directory.GetCurrentDirectory() + "/Sql/RecentAlgorithmVideos.sql");
        var results = this.Set<AlgorithmVideoEntity>()
                       .FromSqlRaw(sql, algorithmId, take, 3)
                       .ToList();
        return results;
    }
}