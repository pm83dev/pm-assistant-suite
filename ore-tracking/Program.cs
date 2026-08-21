using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using OreTracking.Api.Controllers;
using OreTracking.Api.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AdminUserSettings>(builder.Configuration.GetSection("AdminUser"));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Add Authentication with JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSuperSecretKey123!"))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Register Swagger generator using Swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Time Tracking API",
        Version = "v1",
        Description = "API REST per la gestione delle attività e delle ore lavorate"
    });

    // Add JWT Authentication support in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Registra il DbContext con SQLite
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddScoped<IDataRepository, DataService>();



var app = builder.Build();

// Assicura che il database e le tabelle esistano (solo per sviluppo)
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
dbContext.Database.EnsureCreated();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("FrontendCors");

// In produzione: servi il frontend Angular come file statici
var env = app.Environment;
if (env.IsProduction())
{
    app.UseStaticFiles();
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new[] { "index.html" }
    });
    app.Use(async (context, next) =>
    {
        await next();
        if (context.Response.StatusCode == 404 && !context.Request.Path.StartsWithSegments("/api"))
        {
            context.Request.Path = "/index.html";
            await next();
        }
    });
}

app.MapControllers();

app.Run();
