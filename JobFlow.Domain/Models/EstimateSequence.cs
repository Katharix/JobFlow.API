namespace JobFlow.Domain.Models;

public class EstimateSequence : Entity
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int LastSequence { get; set; }
}
