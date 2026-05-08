using JobFlow.Business.DI;
using Microsoft.AspNetCore.DataProtection;

namespace JobFlow.Infrastructure.Integrations.QuickBooks;

public interface IQuickBooksTokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

[SingletonService]
public class QuickBooksTokenEncryptionService : IQuickBooksTokenEncryptionService
{
    private readonly IDataProtector _protector;

    public QuickBooksTokenEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("JobFlow.QuickBooks.OAuthTokens");
    }

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
