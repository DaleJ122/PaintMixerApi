using FluentValidation;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using PaintMixer.Api;
using PaintMixer.Api.Resources;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaintMixerService>();
builder.Services.AddSingleton<IPaintMixerService>(sp => sp.GetRequiredService<PaintMixerService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<PaintMixerService>());

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Localization
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "en-GB", "en-US" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(localizer["TooManyRequests"].Value, token);
    };
});

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

app.UseRequestLocalization();

app.UseStatusCodePages();

// API Endpoints
app.MapPost("/jobs", async (
    SubmitJobRequest req,
    IValidator<SubmitJobRequest> validator,
    IPaintMixerService mixer,
    IStringLocalizer<SharedResource> localizer) =>
    {
        var result = await validator.ValidateAsync(req);
        if (!result.IsValid)
            return Results.ValidationProblem(result.ToDictionary());

        var code = mixer.SubmitJob(req.Red, req.Black, req.White, req.Yellow, req.Blue, req.Green);

        return code == -1
            ? Results.Problem(localizer["JobSubmitFailed"].Value, statusCode: 422)
            : Results.Created($"/jobs/{code}", new JobSubmittedResponse(code));
    })
.Produces<JobSubmittedResponse>(201)
.ProducesValidationProblem()
.ProducesProblem(422);

app.MapGet("/jobs/{jobCode:int}", (
    int jobCode,
    IPaintMixerService mixer,
    IStringLocalizer<SharedResource> localizer) =>
    {
        var state = mixer.QueryJobState(jobCode);

        return state switch
        {
            -1 => Results.NotFound(new MessageResponse(string.Format(localizer["JobNotFoundOrCancelled"].Value, jobCode))),
            0 => Results.Ok(new JobStatusResponse(jobCode, localizer["JobStatusQueuedOrRunning"].Value)),
            1 => Results.Ok(new JobStatusResponse(jobCode, localizer["JobStatusCompleted"].Value)),
            _ => Results.Problem(localizer["UnexpectedDeviceState"].Value, statusCode: 500)
        };
    })
.Produces<JobStatusResponse>()
.Produces<MessageResponse>(404)
.ProducesProblem(500);

app.MapDelete("/jobs/{jobCode:int}", (
    int jobCode,
    IPaintMixerService mixer, 
    IStringLocalizer<SharedResource> localizer) =>
    {
        var result = mixer.CancelJob(jobCode);

        return result == -1
            ? Results.Problem(string.Format(localizer["JobCancelFailed"].Value, jobCode), statusCode: 422)
            : Results.Ok(new MessageResponse(string.Format(localizer["JobCancelledSuccessfully"].Value, jobCode)));
    })
.Produces<MessageResponse>()
.ProducesProblem(422);

app.Run();

public partial class Program { }