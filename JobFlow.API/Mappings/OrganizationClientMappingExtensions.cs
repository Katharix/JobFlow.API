using JobFlow.API.Models;
using JobFlow.Domain.Models;

namespace JobFlow.API.Mappings
{
    public static class OrganizationClientMappingExtensions
    {
        public static OrganizationClientDto ToDto(this OrganizationClient entity)
        {
            if (entity == null) return null!;

            return new OrganizationClientDto
            {
                OrganizationId = entity.OrganizationId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Address1 = entity.Address1,
                Address2 = entity.Address2,
                City = entity.City,
                State = entity.State,
                ZipCode = entity.ZipCode,
                PhoneNumber = entity.PhoneNumber,
                EmailAddress = entity.EmailAddress
            };
        }

        public static OrganizationClient ToEntity(this OrganizationClientDto dto)
        {
            if (dto == null) return null!;

            return new OrganizationClient
            {
                OrganizationId = dto.OrganizationId,
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
