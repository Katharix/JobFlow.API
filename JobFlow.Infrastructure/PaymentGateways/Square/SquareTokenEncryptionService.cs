using JobFlow.Business.DI;
using Microsoft.AspNetCore.DataProtection;

namespace JobFlow.Infrastructure.PaymentGateways.Square;

public interface ISquareTokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

[SingletonService]
public class SquareTokenEncryptionService : ISquareTokenEncryptionService
{
    private readonly IDataProtector _protector;

    public SquareTokenEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("JobFlow.Square.OAuthTokens");
    }

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
