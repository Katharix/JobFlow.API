namespace JobFlow.Business.Models;

public class NewsletterSubscriptionRequest
{
    public string Email { get; set; } = string.Empty;
    public int ListId { get; set; }
    public string CaptchaToken { get; set; } = string.Empty;
}