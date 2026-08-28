using System.Text;
using Collaborate.Api.Authorization;
using Collaborate.Api.Observability;
using Collaborate.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Caseware Collaborate Authorization Slice",
        Version = "v1",
        Description = "Part 2 Option A implementation of a protected document resource endpoint with JWT-based access checks."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT bearer token used to access protected Collaborate API endpoints.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
var authenticationSection = builder.Configuration.GetSection(AuthenticationOptions.SectionName);
var issuer = authenticationSection["Issuer"] ?? throw new InvalidOperationException("Authentication issuer is not configured.");
var audience = authenticationSection["Audience"] ?? throw new InvalidOperationException("Authentication audience is not configured.");
var signingKey = authenticationSection["SigningKey"] ?? throw new InvalidOperationException("Authentication signing key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.DocumentRead,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new DocumentReadRequirement());
        });
});
builder.Services.AddSingleton<IDocumentService, DocumentService>();
builder.Services.AddSingleton<IAuthorizationHandler, DocumentReadAccessContextAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DocumentReadResourceAuthorizationHandler>();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "Collaborate.Api",
        serviceVersion: "0.1.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource(CollaborateObservability.ActivitySourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(CollaborateObservability.MeterName)
        .AddConsoleExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Caseware Collaborate Authorization Slice v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();