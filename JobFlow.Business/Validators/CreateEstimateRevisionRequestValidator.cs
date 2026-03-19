using FluentValidation;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Validators;

public class CreateEstimateRevisionRequestValidator : AbstractValidator<CreateEstimateRevisionRequest>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain"
    ];

    public CreateEstimateRevisionRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Attachments)
            .NotNull();

        RuleFor(x => x.Attachments.Count)
            .LessThanOrEqualTo(5);

        RuleForEach(x => x.Attachments).ChildRules(attachment =>
        {
            attachment.RuleFor(x => x.FileName)
                .NotEmpty()
                .MaximumLength(260);

            attachment.RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(contentType => AllowedContentTypes.Contains(contentType))
                .WithMessage("Unsupported attachment content type.");

            attachment.RuleFor(x => x.Content)
                .NotNull()
                .Must(content => content.Length > 0)
                .WithMessage("Attachment content is required.");

            attachment.RuleFor(x => x.SizeBytes)
                .GreaterThan(0)
                .LessThanOrEqualTo(10 * 1024 * 1024);

            attachment.RuleFor(x => x)
                .Must(x => x.Content.Length == x.SizeBytes)
                .WithMessage("Attachment size does not match payload size.");
        });
    }
}
