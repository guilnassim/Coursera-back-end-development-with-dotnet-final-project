using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechHive.UserManagement.Application;
using TechHive.UserManagement.Application.Classes;
using TechHive.UserManagement.Application.Interfaces;
using TechHive.UserManagement.Application.POCO;
using TechHive.UserManagement.Infrastructure;
using UserManagementAPI.Contracts;
using UserManagementAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ==== Configuration (JWT) ====
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;
var jwtSecret = jwtSection["Secret"]!;
var jwtLifetimeMinutes = int.TryParse(jwtSection["TokenLifetimeMinutes"], out var minutes) ? minutes : 30;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// ==== Services ====
builder.Services.AddLogging();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Swagger/OpenAPI with JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechHive User Management API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// AuthN/Z
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// ==== Build app ====
var app = builder.Build();

// ==== Middleware pipeline (order matters) ====
// 1) Error handling (first)
app.UseMiddleware<ErrorHandlingMiddleware>();

// You can keep HTTPS redirection right after error handling (safe & recommended)
app.UseHttpsRedirection();

// 2) Authentication & Authorization (next)
app.UseAuthentication();
app.UseAuthorization();


// 3) Lightweight security (content-type + size; then headers)
// BEFORE Swagger so responses from Swagger also get headers.
app.UseMiddleware<LightweightRequestGuardMiddleware>();
app.UseMiddleware<LightweightSecurityHeadersMiddleware>();


// Swagger UI (available in Development; move outside if you want always-on)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4) Logging (last, as requested)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// ==== Endpoints ====

// Health/info
app.MapGet("/", () => Results.Ok(new { app = "TechHive UserManagementAPI", version = "v1" }));

// Dev token issuing endpoint (for testing only)
app.MapPost("/auth/token", [AllowAnonymous] (TokenRequest req) =>
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, req.Subject),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("scope", "users.read users.write")
    };

    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddMinutes(jwtLifetimeMinutes),
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = jwt });
})
.WithDescription("DEV ONLY: issues a short-lived JWT");

// Users endpoints (protected)
var users = app.MapGroup("/api/users").RequireAuthorization();

// List with pagination/filtering
users.MapGet("/", async (
    IUserService svc,
    string? department,
    bool? isActive,
    int? page,
    int? pageSize,
    CancellationToken ct) =>
{
    var result = await svc.GetPagedAsync(department, isActive, page ?? 1, pageSize ?? 20, ct);
    return Results.Ok(result);
})
.WithSummary("List users with optional filtering and pagination")
.WithOpenApi();

// Get by id
users.MapGet("/{id:int}", async (int id, IUserService svc, CancellationToken ct) =>
{
    var user = await svc.GetByIdAsync(id, ct);
    return user is not null
        ? Results.Ok(user)
        : Results.NotFound(new { error = $"User {id} not found." });
})
.WithSummary("Get a user by id")
.WithOpenApi();

// Create
users.MapPost("/", async (CreateUserRequest request, IUserService svc, CancellationToken ct) =>
{
    var id = await svc.CreateAsync(request, ct);
    return Results.Created($"/api/users/{id}", new { id });
})
.WithSummary("Create a new user")
.WithOpenApi();

// Update
users.MapPut("/{id:int}", async (int id, UpdateUserRequest request, IUserService svc, CancellationToken ct) =>
{
    var ok = await svc.UpdateAsync(id, request, ct);
    return ok ? Results.NoContent() : Results.NotFound(new { error = $"User {id} not found." });
})
.WithSummary("Update an existing user")
.WithOpenApi();

// Delete
users.MapDelete("/{id:int}", async (int id, IUserService svc, CancellationToken ct) =>
{
    var ok = await svc.DeleteAsync(id, ct);
    return ok ? Results.NoContent() : Results.NotFound(new { error = $"User {id} not found." });
})
.WithSummary("Delete a user by id")
.WithOpenApi();

// Simulate a failure for testing error middleware
users.MapGet("/boom", (context) => throw new InvalidOperationException("Simulated failure."))
     .WithSummary("Test error middleware")
     .WithOpenApi();

app.Run();
