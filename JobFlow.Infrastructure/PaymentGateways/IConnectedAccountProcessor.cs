namespace JobFlow.Infrastructure.PaymentGateways;

public interface IConnectedAccountProcessor
{
    Task<string> CreateConnectedAccountAsync();
    Task<string> GenerateAccountLinkAsync(string accountId, string? returnUrl = null, string? refreshUrl = null);
}