using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using BabaiBazaar.API.Data;
using BabaiBazaar.API.Helpers;

// ── SERILOG ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/babai-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── DATABASE ──────────────────────────────────────────────────
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(conn, ServerVersion.AutoDetect(conn),
        x => x.EnableRetryOnFailure(3)));

// ── JWT AUTH ──────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────
var origins = (builder.Configuration["AppSettings:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ── SWAGGER ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Babai Bazaar API",
        Version     = "v1",
        Description = "Quick Commerce & Home Services Platform API — Mahvenx IT Solutions Pvt. Ltd.",
        Contact     = new OpenApiContact { Name = "Babai Bazaar Support" }
    });

    // JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((_, _) => true);
});

// ── SERVICES ──────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy         = null;  // keep PascalCase
        opts.JsonSerializerOptions.DefaultIgnoreCondition       = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.ReferenceHandler             = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<UploadHelper>();
builder.Services.AddScoped<ICloudflareService, CloudflareService>();
builder.Services.AddMemoryCache();

// ── APP BUILD ─────────────────────────────────────────────────
var app = builder.Build();


// ── SEED DATABASE ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DataSeeder.SeedAsync(db);
        Log.Information("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database seeding failed — check connection string. Starting anyway.");
    }
}

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────
app.UseSerilogRequestLogging();

// Swagger — available in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Babai Bazaar API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Babai Bazaar API";
});

app.UseStaticFiles();        // serves /wwwroot/uploads/*
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/", () => new
{
    status  = "Babai Bazaar API is running",
    version = "v1.0.0",
    env     = app.Environment.EnvironmentName,
    time    = DateTime.UtcNow,
    docs    = "/swagger"
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

Log.Information("Babai Bazaar API starting on port 8085...");
app.Run();
