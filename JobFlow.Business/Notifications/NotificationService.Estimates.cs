using JobFlow.Domain.Models;

namespace JobFlow.Business.Notifications;

public partial class NotificationService
{
    public async Task SendClientEstimateSentNotificationAsync(OrganizationClient client, Estimate estimate)
    {
        var message = _builder.BuildClientEstimateSent(client, estimate);
        await SendNotificationAsync(message);
    }
}