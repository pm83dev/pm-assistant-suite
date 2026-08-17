using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using OreTracking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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
});

// Registra il DbContext con SQLite
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddScoped<IDataRepository, DataService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Assicura che il database e le tabelle esistano (solo per sviluppo)
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
dbContext.Database.EnsureCreated();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

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
