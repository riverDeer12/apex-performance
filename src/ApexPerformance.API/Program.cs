using ApexPerformance.API;
using ApexPerformance.API.BackgroundJobs;
using ApexPerformance.API.Database;
using ApexPerformance.API.Extensions;
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
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configuration = builder.Configuration;

builder.Services
    .AddAuthenticationJwtBearer(s =>
        s.SigningKey = configuration["JWTSecretKey"])
    .AddAuthorization()
    .AddFastEndpoints();

builder.Services.AddDbContext<ApexPerformanceContext>(options =>
    options
        .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .EnableSensitiveDataLogging());

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

// Background Jobs
builder.Services.AddScoped<IEmailJob, EmailJob>();

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

app.UseFastEndpoints();

app.UseAuthentication();

app.UseAuthorization();

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

app.UseSerilogRequestLogging();

app.UseMiddleware<AdditionalRequestLogging>();

app.UseHttpsRedirection();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthPolicy() }
});

app.Run();

public partial class Program
{
}