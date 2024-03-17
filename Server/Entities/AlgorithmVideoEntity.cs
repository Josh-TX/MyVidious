using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

//should match the output of the proc GetRandomAlgorithmVideos
public class AlgorithmVideoEntity
{
    public int ChannelId { get; set; }
    public int VideoId { get; set; }
    public double InMemoryFactorIncrease { get; set; }
}