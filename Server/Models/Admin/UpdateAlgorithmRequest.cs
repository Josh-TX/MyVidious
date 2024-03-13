namespace MyVidious.Models.Admin;

public class UpdateAlgorithmRequest
{
    public int? AlgorithmId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public IEnumerable<UpdateAlgorithmItem> AlgorithmItems { get; set; }
}