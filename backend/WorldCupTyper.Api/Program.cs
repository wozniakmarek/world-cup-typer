using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WorldCupTyper.Api.Health;
using WorldCupTyper.Api.Middleware;
using WorldCupTyper.Infrastructure;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Persistence;
using WorldCupTyper.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins =
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:4173",
        "http://127.0.0.1:4173",
        "https://typer.marekwozniak.me",
    ];
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            message = "Dane wejściowe są niepoprawne.",
            errors,
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WorldCupTyperDbContext>("database");
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "World Cup Typer API",
        Version = "v1",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wpisz token JWT w formacie: Bearer {token}",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddWorldCupTyperInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("JWT key is missing. Configure Jwt:Key in appsettings or environment variables.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "name",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PasswordChangeMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync,
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync,
});
app.MapControllers();

app.Run();
