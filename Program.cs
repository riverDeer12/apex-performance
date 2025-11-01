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
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRecurringAppointmentService, RecurringAppointmentService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IUserService, UserService>();

// Background Jobs
builder.Services.AddScoped<IEmailJob, EmailJob>();

builder.Host.UseSerilog((context, config)
    => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSingleton<ProductService>(sp =>
    new ProductService(sp.GetRequiredService<StripeClient>()));
builder.Services.AddSingleton<PriceService>(sp =>
    new PriceService(sp.GetRequiredService<StripeClient>()));

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.UseSerilogRequestLogging();

app.UseMiddleware<AdditionalRequestLogging>();

app.UseHttpsRedirection();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthPolicy() }
});

app.Run();