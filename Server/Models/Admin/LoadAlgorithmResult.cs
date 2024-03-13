namespace MyVidious.Models.Admin;

public class LoadAlgorithmResult
{
    public int? AlgorithmId { get; set; }
    public string Username { get; set; }
    public string AlgorithmName { get; set; }
    public string? Description { get; set; }
    public IEnumerable<LoadAlgorithmItem> AlgorithmItems { get; set; }
}
