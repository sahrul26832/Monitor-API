using System.Data;
using DemoResendInterface.Data;
using DemoResendInterface.Middleware;
using DemoResendInterface.Repositories;
using DemoResendInterface.Repositories.Interfaces;
using DemoResendInterface.Services;
using DemoResendInterface.Services.Interfaces;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ─── Dependency Injection ─────────────────────────────────────────────
// Database connection (scoped per request)
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
});

// Repositories
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IErrorRepository, ErrorRepository>();

// Services
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IErrorService, ErrorService>();

// Database initializer
builder.Services.AddSingleton<DatabaseInitializer>();

// HttpClient for resend functionality
builder.Services.AddHttpClient();

// Controllers + JSON options
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ─── Database Initialization ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInit.InitializeAsync();
}

// ─── Middleware Pipeline ──────────────────────────────────────────────
// Global exception handler (must be first to catch all errors)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS
app.UseCors();

// Serve static files (index.html, styles.css, app.js from wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// API routes
app.MapControllers();

// Health check
app.MapGet("/healthz", () => Results.Json(new { status = "ok" }));

// SPA fallback — serve index.html for any non-API route
app.MapFallbackToFile("index.html");

app.Run();
