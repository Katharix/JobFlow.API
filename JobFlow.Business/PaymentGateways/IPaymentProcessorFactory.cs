using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways
{
    public interface IPaymentProcessorFactory
    {
        IPaymentProcessor GetProcessor(string provider);
        IPaymentProcessor GetProcessor(PaymentProvider provider);
    }

}
