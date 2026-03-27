using JobFlow.Domain.Models;

namespace JobFlow.Business.Notifications;

public partial class NotificationService
{
    public async Task SendClientEstimateSentNotificationAsync(OrganizationClient client, Estimate estimate)
    {
        var message = _builder.BuildClientEstimateSent(client, estimate);
        await SendNotificationAsync(message);
    }

    public async Task SendClientEstimateFollowUpNotificationAsync(OrganizationClient client, Estimate estimate, string followUpMessage)
    {
        var message = _builder.BuildClientEstimateFollowUp(client, estimate, followUpMessage);
        await SendNotificationAsync(message);
    }
}