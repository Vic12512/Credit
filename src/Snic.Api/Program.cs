using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Snic.Api.Infrastructure;
using Snic.Api.Repositories;
using Snic.Api.Services;

EnvFile.LoadForApiProject();
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var connectionString = builder.Configuration.GetConnectionString("SnicDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings__SnicDb.");
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Configure Jwt__Key with at least 32 characters.");

builder.Services.AddControllers(options => options.Filters.Add<ApiResponseFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ApiErrors.Create(context.HttpContext, 400, errors:
            context.ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(x => x.Key,
                x => x.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "El valor enviado no es válido." : error.ErrorMessage).ToArray()))));
builder.Services.AddScoped<RequestAuditState>();
builder.Services.AddScoped<ApiResponseFilter>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API SNIC",
        Version = "v1",
        Description = "Sistema académico de información crediticia. Autenticación JWT, gestión institucional, personas, productos, consulta consolidada con indicadores simulados y auditoría. Los indicadores no constituyen una evaluación financiera real."
    });
    options.OperationFilter<SwaggerPermissionsOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Ingrese el token JWT obtenido en /api/v1/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddDbContext<SnicDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<ICreditProductRepository, CreditProductRepository>();
builder.Services.AddScoped<IPasswordService, Pbkdf2PasswordService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<DevelopmentAdminBootstrapper>();
builder.Services.AddScoped<IInstitutionService, InstitutionService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<ICreditProductService, CreditProductService>();
builder.Services.AddScoped<IAcademicIndicatorService, AcademicIndicatorService>();
builder.Services.AddScoped<IAcademicIndicatorRepository, AcademicIndicatorRepository>();
builder.Services.AddScoped<BackendQueryService>();
builder.Services.AddScoped<IBackendQueryRepository, BackendQueryRepository>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5173")
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.IncludeErrorDetails = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Snic.Api",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Snic.Web",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idValue, out var accountId))
            {
                context.HttpContext.Items["AuthErrorCode"] = "INVALID_TOKEN";
                context.Fail("El token no contiene una cuenta válida.");
                return;
            }

            var repository = context.HttpContext.RequestServices.GetRequiredService<IUserAccountRepository>();
            var account = await repository.FindByIdAsync(accountId, false, context.HttpContext.RequestAborted);
            if (account is null || !account.IsActive || !account.Role.IsActive ||
                account.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
            {
                context.HttpContext.Items["AuthErrorCode"] = "ACCOUNT_UNAVAILABLE";
                context.Fail("La cuenta está inactiva o bloqueada.");
            }
        },
        OnAuthenticationFailed = context =>
        {
            context.HttpContext.Items["AuthErrorCode"] = context.Exception is SecurityTokenExpiredException ? "TOKEN_EXPIRED" : "INVALID_TOKEN";
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            var code = context.HttpContext.Items["AuthErrorCode"] as string ?? "AUTHENTICATION_REQUIRED";
            var message = code switch
            {
                "TOKEN_EXPIRED" => "El token ha vencido. Inicia sesión nuevamente.",
                "INVALID_TOKEN" => "El token no es válido.",
                "ACCOUNT_UNAVAILABLE" => "La cuenta está inactiva, bloqueada o ya no existe.",
                _ => "Se requiere un token para acceder."
            };
            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(ApiErrors.Create(context.HttpContext, 401, message, code));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(ApiErrors.Create(context.HttpContext, 403));
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddDbContextCheck<SnicDbContext>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentAdminBootstrapper>().InitializeAsync();
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    await response.WriteAsJsonAsync(ApiErrors.Create(context.HttpContext, response.StatusCode));
});
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<RequestAuditMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
