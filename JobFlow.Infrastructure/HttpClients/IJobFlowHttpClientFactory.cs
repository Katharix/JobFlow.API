using System.Net.Http;

namespace JobFlow.Infrastructure.HttpClients
{
    public interface IJobFlowHttpClientFactory
    {
        HttpClient ForBrevoClient();
    }
}
