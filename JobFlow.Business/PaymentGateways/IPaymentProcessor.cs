using JobFlow.Business.PaymentGateways.SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways
{
    public interface IPaymentProcessor
    {
        Task<string> CreateCheckoutSessionAsync(PaymentSessionRequest request);
    }

}
