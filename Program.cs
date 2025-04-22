using ApexPerformance.API.Database;
using ApexPerformance.API.Middlewares;
using ApexPerformance.API.Services;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Host.UseSerilog((context, config) 
    => config.ReadFrom.Configuration(context.Configuration));

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

app.Run();