namespace JobFlow.Business.Models;

public class ContactFormRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? CaptchaToken { get; set; }
    public int? TemplateId { get; set; }
    public string? Link { get; set; }
}