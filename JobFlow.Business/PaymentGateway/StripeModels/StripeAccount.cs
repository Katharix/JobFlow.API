using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateway.StripeModels
{
    public class StripeAccount
    {
        public string? Country { get; set; }
        public string? Email { get; set; }
        public AccountController Controller { get; set; }

    }
    public class AccountController
    {
        public AccountControllerFees Fees { get; set; }
        public AccountControllerLosses Loses { get; set; }
        public AccountControllerStripeDashboard StripeDashboard { get; set; }
    }
    public class AccountControllerFees
    {
        public string Payer { get; set; }
    }
    public class AccountControllerLosses
    {
        public string Payments { get; set; }
    }
    public class AccountControllerStripeDashboard
    {
        public string Type { get; set; }
    }
    public enum BusinessType
    {
        Company,
        GovernmentEntity,
        Individual,
        NonProfit
    }
    public enum StripeAccountType
    {
        Custom,
        Express,
        Standard
    }
}
