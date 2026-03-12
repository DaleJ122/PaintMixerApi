using FluentValidation;
using Microsoft.Extensions.Localization;
using PaintMixer.Api.Resources;

namespace PaintMixer.Api
{
    public class SubmitJobRequestValidator : AbstractValidator<SubmitJobRequest>
    {
        public SubmitJobRequestValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.Red).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "Red", 0, 100));
            RuleFor(x => x.Black).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "Black", 0, 100));
            RuleFor(x => x.White).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "White", 0, 100));
            RuleFor(x => x.Yellow).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "Yellow", 0, 100));
            RuleFor(x => x.Blue).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "Blue", 0, 100));
            RuleFor(x => x.Green).InclusiveBetween(0, 100)
                .WithMessage(x => string.Format(localizer["DyeMustBeBetween"].Value, "Green", 0, 100));

            RuleFor(x => x.Red + x.Black + x.White + x.Yellow + x.Blue + x.Green)
                .LessThanOrEqualTo(100)
                .WithMessage(x => localizer["TotalDyeExceeds100"].Value);
        }
    }
}
