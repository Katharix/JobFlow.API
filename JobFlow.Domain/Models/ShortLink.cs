namespace JobFlow.Domain.Models;

public class ShortLink : Entity
{
    public string Code { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
