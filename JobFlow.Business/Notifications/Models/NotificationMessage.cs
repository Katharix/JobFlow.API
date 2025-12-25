using JobFlow.Business.Notifications.Enums;

namespace JobFlow.Business.Notifications.Models;

public class NotificationMessage
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Sms { get; set; }
    public string? Link { get; set; }
    public EmailTemplate? TemplateId { get; set; }
}