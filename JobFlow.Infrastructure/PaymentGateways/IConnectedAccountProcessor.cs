namespace JobFlow.Infrastructure.PaymentGateways;

public interface IConnectedAccountProcessor
{
    Task<string> CreateConnectedAccountAsync();
    Task<string> GenerateAccountLinkAsync(string accountId);
}