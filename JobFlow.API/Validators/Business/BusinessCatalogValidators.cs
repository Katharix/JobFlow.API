using JobFlow.Business.Models.DTOs;

namespace JobFlow.API.Validators;

public sealed class OrganizationDtoValidator : SafeRequestValidator<OrganizationDto>
{
    public OrganizationDtoValidator() : base("Name") { }
}

public sealed class PriceBookItemBusinessDtoValidator : SafeRequestValidator<JobFlow.Business.Models.DTOs.PriceBookItemDto>
{
    public PriceBookItemBusinessDtoValidator() : base("Name") { }
}
