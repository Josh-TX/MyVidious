namespace MyVidious.Models.Admin;

public class FoundAlgorithm
{
    public int AlgorithmId { get; set; }
    public required string Username { get; set; }
    public required string AlgorithmName { get; set; }
    public string? Description { get; set; }
}
