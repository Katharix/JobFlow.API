using FluentValidation;
using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;

namespace JobFlow.Business.Validators;

public class CreateJobUpdateRequestValidator : AbstractValidator<CreateJobUpdateRequest>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public CreateJobUpdateRequestValidator()
    {
        RuleFor(x => x.Attachments)
            .NotNull();

        RuleFor(x => x.Type)
            .IsInEnum();

        When(x => x.Type == JobUpdateType.Note, () =>
        {
            RuleFor(x => x.Message)
                .NotEmpty()
                .MaximumLength(2000);
        });

        When(x => x.Type == JobUpdateType.StatusChange, () =>
        {
            RuleFor(x => x.Status)
                .NotNull();
        });

        When(x => x.Type == JobUpdateType.Photo, () =>
        {
            RuleFor(x => x.Attachments.Count)
                .GreaterThan(0)
                .LessThanOrEqualTo(6);

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
                    .LessThanOrEqualTo(15 * 1024 * 1024);

                attachment.RuleFor(x => x)
                    .Must(x => x.Content.Length == x.SizeBytes)
                    .WithMessage("Attachment size does not match payload size.");
            });
        });

        When(x => x.Type != JobUpdateType.Photo, () =>
        {
            RuleFor(x => x.Attachments.Count)
                .LessThanOrEqualTo(0);
        });
    }
}
