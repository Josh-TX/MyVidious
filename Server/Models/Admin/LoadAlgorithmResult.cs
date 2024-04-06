namespace MyVidious.Models.Admin;

public class LoadAlgorithmResult
{
    public int AlgorithmId { get; set; }
    public required string Username { get; set; }
    public required string AlgorithmName { get; set; }
    public required int MaxItemWeight { get; set; }
    public string? Description { get; set; }
    public required IEnumerable<LoadAlgorithmItem> AlgorithmItems { get; set; }
    public required double EstimatedSumWeight {get;set;}
}
