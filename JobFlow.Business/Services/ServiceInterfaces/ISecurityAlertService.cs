namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISecurityAlertService
{
    Task EvaluateRecentEventsAsync();
}
