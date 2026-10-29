using ApexPerformance.API.Features.Shop;
using ApexPerformance.API;
using ApexPerformance.API.BackgroundJobs;
using ApexPerformance.API.Database;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Middlewares;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Implementation;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Stripe;
using ApexPerformance.API.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configuration = builder.Configuration;

builder.Services
    .AddAuthenticationJwtBearer(s =>
            s.SigningKey = configuration["JWTSecretKey"],
        o => o.Events = new JwtBearerEvents
        {
            // Browsers can't send headers on WebSocket connections,
            // so the chat hub gets the token in the query string.
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(ChatHub.Path))
                    context.Token = token;

                return Task.CompletedTask;
            },
            OnTokenValidated = UserSessionTokenValidation.ValidateAsync
        })
    .AddAuthorization()
    .AddFastEndpoints();

builder.Services.AddDbContext<ApexPerformanceContext>(options =>
    options
        .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .EnableSensitiveDataLogging()
        // A client leaving while the connection opens is logged by EF as a connection error and
        // would send an alert email. Real outages still log errors: the request fails with 500.
        .ConfigureWarnings(warnings => warnings.Log((RelationalEventId.ConnectionError, LogLevel.Warning))));

// Business Services
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IRecurringAppointmentService, RecurringAppointmentService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPersonalRecordService, PersonalRecordService>();
builder.Services.AddScoped<ShopSettings>();
builder.Services.AddScoped<ShopOrderService>();
builder.Services.AddScoped<ShopPaymentEvents>();
builder.Services.AddSingleton<IChatEncryptionService, ChatEncryptionService>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SubClaimUserIdProvider>();

builder.Services.AddApiRateLimiting();

// Background Jobs
builder.Services.AddScoped<IEmailJob, EmailJob>();
builder.Services.AddScoped<DeclineExpiredRequestsJob>();
builder.Services.AddScoped<MonthlyReviewReminderJob>();
builder.Services.AddScoped<ErrorAlertJob>();
builder.Services.AddScoped<ShopOrderSyncJob>();
builder.Services.AddScoped<TrainingNotificationJob>();

builder.Host.UseSerilog((context, config)
    => config.ReadFrom.Configuration(context.Configuration));

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(configuration["Firebase:CredentialsPath"])
});

builder.Services.AddSingleton<IFcmService, FcmService>();

builder.Services.AddSingleton<ProductService>(sp =>
    new ProductService(sp.GetRequiredService<StripeClient>()));

builder.Services.AddSingleton<PriceService>(sp =>
    new PriceService(sp.GetRequiredService<StripeClient>()));

builder.Services.AddSingleton<INotificationService, NotificationService>();


builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                SchemaName = "hangfire",
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                UseRecommendedIsolationLevel = true
            });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
    options.Queues = new[] { "default", "critical" };
});

var app = builder.Build();

app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseRateLimiter();

app.UseFastEndpoints();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<ChatHub>(ChatHub.Path);

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        // Only root-level bundle files (main-*.js, chunk-*.js, styles-*.css, ...) get
        // content-hashed names from the Angular build - those can be cached forever.
        // Files under assets/ or media/ keep their original names across builds, so a
        // long immutable cache would mask real content updates the same way index.html did.
        ctx.Context.Response.Headers.CacheControl =
            ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase)
                ? "no-cache, no-store, must-revalidate"
                : path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/media/", StringComparison.OrdinalIgnoreCase)
                    ? "public, max-age=3600, must-revalidate"
                    : "public, max-age=31536000, immutable";
    }
};

app.UseStaticFiles(staticFileOptions);

app.MapFallbackToFile("index.html", staticFileOptions);

app.UseSerilogRequestLogging(options =>
{
    // A request the client gave up on (left the screen, lost the connection) ends with
    // a 500 nobody sees; it is a warning, not an error of the application.
    options.GetLevel = (httpContext, _, exception) =>
        httpContext.RequestAborted.IsCancellationRequested
            ? LogEventLevel.Warning
            : exception is not null || httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : LogEventLevel.Information;
});

app.UseMiddleware<AdditionalRequestLogging>();

app.UseHttpsRedirection();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthPolicy(app.Configuration) }
});

DeclineExpiredRequestsJob.Schedule();
MonthlyReviewReminderJob.ScheduleJob();
ErrorAlertJob.ScheduleJob(app.Environment, app.Configuration);
ShopOrderSyncJob.ScheduleJob();

// Records of trainings completed before personal records existed.
BackgroundJob.Enqueue<IPersonalRecordService>(service => service.CalculateMissingRecordsAsync());

// Muscles of workouts added before muscles existed, guessed from their names.
BackgroundJob.Enqueue<WorkoutMusclesJob>(job => job.FillMissingAsync());

app.Run();

public partial class Program
{
}