namespace MyVidious.Models.Admin;

public class UpdateAlgorithmRequest
{
    /// <summary>
    /// When null, means you're creating a new algorithm
    /// </summary>
    public int? AlgorithmId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required IEnumerable<UpdateAlgorithmItem> AlgorithmItems { get; set; }
}