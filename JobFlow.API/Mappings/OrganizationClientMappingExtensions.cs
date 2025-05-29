using JobFlow.API.Models;
using JobFlow.Domain.Models;

namespace JobFlow.API.Mappings
{
    public static class OrganizationClientMappingExtensions
    {
        public static OrganizationClientDto ToDto(this OrganizationClient client) =>
            new OrganizationClientDto
            {
                Id = client.Id,
                OrganizationId = client.OrganizationId,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Address1 = client.Address1,
                Address2 = client.Address2,
                City = client.City,
                State = client.State,
                ZipCode = client.ZipCode,
                PhoneNumber = client.PhoneNumber,
                EmailAddress = client.EmailAddress,
                Organization = client.Organization.ToDto(),
            };

        public static OrganizationDto ToDto(this Organization org) =>
           new OrganizationDto
           {
               Id = org.Id,
               OrganizationName = org.OrganizationName,
               Address1 = org.Address1,
               Address2 = org.Address2,
               City = org.City,
               State = org.State,
               ZipCode = org.ZipCode,
               PhoneNumber = org.PhoneNumber,
               Email = org.EmailAddress,
               OnBoardingComplete = org.OnBoardingComplete,
           };
        public static OrganizationClient ToEntity(this OrganizationClientDto dto)
        {
            if (dto == null) return null!;

            return new OrganizationClient
            {
                OrganizationId = dto.OrganizationId.Value,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                PhoneNumber = dto.PhoneNumber,
                EmailAddress = dto.EmailAddress
            };
        }
    }
}
