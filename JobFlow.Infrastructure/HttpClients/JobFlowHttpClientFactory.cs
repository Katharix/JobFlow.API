using JobFlow.Business.DI;

namespace JobFlow.Infrastructure.HttpClients;

[SingletonService]
public class JobFlowHttpClientFactory : IJobFlowHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JobFlowHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient ForBrevoClient()
    {
        return _httpClientFactory.CreateClient(JobFlowNamedClient.Brevo);
    }
}