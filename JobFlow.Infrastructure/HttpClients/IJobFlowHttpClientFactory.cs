namespace JobFlow.Infrastructure.HttpClients;

public interface IJobFlowHttpClientFactory
{
    HttpClient ForBrevoClient();
}