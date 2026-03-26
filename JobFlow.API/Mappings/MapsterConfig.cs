using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;
using Mapster;

namespace JobFlow.API.Mappings;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // EmployeeInvite → DTO
        config.NewConfig<EmployeeInvite, EmployeeInviteDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}".Trim());

        // DTO → EmployeeInvite
        config.NewConfig<EmployeeInviteDto, EmployeeInvite>();

        // Employee → DTO
        config.NewConfig<Employee, EmployeeDto>()
            .Map(dest => dest.Role, src => src.RoleId);

        // DTO → Employee
        config.NewConfig<EmployeeDto, Employee>();

        //EmployeeRole → DTO
        config.NewConfig<EmployeeRole, EmployeeRoleDto>();

        // Organization → DTO
        config.NewConfig<Organization, OrganizationDto>();

        //OrganizationBranding → DTO
        config.NewConfig<OrganizationBranding, BrandingDto>();

        //OrganizationClient → DTO
        config.NewConfig<OrganizationClient, OrganizationClientDto>();

        //DTO → OrganizationClient
        config.NewConfig<OrganizationClientDto, OrganizationClient>()
            .Ignore(dest => dest.Organization);

        //Invoice → DTO
        config.NewConfig<Invoice, InvoiceDto>();

        //InvoiceLineItem → DTO
        config.NewConfig<InvoiceLineItem, InvoiceLineItemDto>();

        //Job → DTO
        config.NewConfig<Job, JobDto>();

        config.NewConfig<Assignment, AssignmentDto>();
        //DTO → Job
        config.NewConfig<JobDto, Job>();

        //OnboardingDto → Organization
        config.NewConfig<OnboardingDto, OrganizationOnboardingStep>();
    }
}