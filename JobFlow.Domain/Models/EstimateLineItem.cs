namespace JobFlow.Domain.Models;

public class EstimateLineItem : Entity
{
    public Guid EstimateId { get; set; }
    public Guid? PriceBookItemId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public Estimate? Estimate { get; set; }
    public PriceBookItem? PriceBookItem { get; set; }
}