using JobFlow.Infrastructure.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.HttpClients
{
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

}
