using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

public class AlgorithmVideoEntity
{
    public int ChannelId { get; set; }
    public int VideoId { get; set; }
    public double ChannelPercent { get; set; }
}