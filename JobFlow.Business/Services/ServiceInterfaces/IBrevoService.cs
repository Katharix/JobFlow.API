using JobFlow.Business.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IBrevoService
{
    Task<bool> AddContactAsync(string email, int listId);
    Task<bool> SendContactEmailAsync(ContactFormRequest request);
}