using JobFlow.Business.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IBrevoService
{
    Task<bool> AddContactAsync(string email, int listId);
    Task<bool> SendContactEmailAsync(ContactFormRequest request);
    Task<bool> AddTrialContactAsync(string email, string firstName, string lastName, string orgName, DateTimeOffset trialStartDate);
    Task TrackActivationEventAsync(string email, string eventKey);
}