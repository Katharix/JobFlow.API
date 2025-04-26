using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.PaymentGateways
{
    public interface IConnectedAccountProcessor
    {
        Task<string> CreateConnectedAccountAsync();
        Task<string> GenerateAccountLinkAsync(string accountId);
    }

}
