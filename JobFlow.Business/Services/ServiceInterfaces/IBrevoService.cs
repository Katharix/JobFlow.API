using JobFlow.Business.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IBrevoService
    {
        Task<bool> AddContactAsync(string email, int listId);
        Task<bool> SendContactEmailAsync(ContactFormRequest request);
    }
}
