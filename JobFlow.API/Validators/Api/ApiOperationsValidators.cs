using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.API.Validators;

public sealed class CustomerPaymentProfileDtoValidator : SafeRequestValidator<CustomerPaymentProfileDto>
{
    public CustomerPaymentProfileDtoValidator() : base("Provider") { }
}

public sealed class InvoiceDtoValidator : SafeRequestValidator<InvoiceDto>
{
    public InvoiceDtoValidator() : base("InvoiceNumber") { }
}

public sealed class PriceBookItemDtoValidator : SafeRequestValidator<JobFlow.Business.Models.DTOs.PriceBookItemDto>
{
    public PriceBookItemDtoValidator() : base("Name") { }
}

public sealed class BrandingDtoValidator : SafeRequestValidator<BrandingDto>
{
    public BrandingDtoValidator() : base("PrimaryColor") { }
}

public sealed class ScheduleJobRequestValidator : SafeRequestValidator<ScheduleJobRequest>
{
    public ScheduleJobRequestValidator() : base("CronExpression") { }
}

public sealed class CreateInvoiceRequestValidator : SafeRequestValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator() : base("CustomerId") { }
}
