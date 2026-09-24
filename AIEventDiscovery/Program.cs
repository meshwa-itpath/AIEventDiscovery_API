using System.Text;
using AIEventDiscovery.Configuration;
using AIEventDiscovery.Data;
using AIEventDiscovery.Data.Repositories;
using AIEventDiscovery.Middleware;
using AIEventDiscovery.Services;
using AIEventDiscovery.Services.DataImport;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.Interfaces;
using AIEventDiscovery.Services.LLM;
using AIEventDiscovery.Services.PgVector;
using AIEventDiscovery.Services.RAG;
using AIEventDiscovery.Services.Search;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configure CORS to allow Angular frontend (default port 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Swagger/OpenAPI with JWT Bearer support so you can test
// protected endpoints directly from the Swagger UI using the 🔒 Authorize button.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define the "Bearer" security scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token below. Example: eyJhbGci..."
    });
});

// PostgreSQL via EF Core with pgvector support
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseVector()));

builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddHttpClient<IGroqService, GroqService>();

builder.Services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();
builder.Services.AddScoped<IPgVectorService, PgVectorService>();
builder.Services.AddScoped<IExtractMetadataFromQueryService, ExtractMetadataFromQueryService>();

// Data import and authentication services
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRetrievalService, RetrievalService>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();

// Generic repository — registered as an open generic so any IGenericRepository<TEntity>
// can be injected without registering each entity separately
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Options binding
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// ──────────────────────────────────────────────────────────────────
// JWT Authentication
// ──────────────────────────────────────────────────────────────────

// Read JWT settings from appsettings.json
var jwtKey      = builder.Configuration["Jwt:Key"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(options =>
    {
        // Use JwtBearer as the default scheme for both authenticating and challenging
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,  // Verify the "iss" claim matches our issuer
            ValidateAudience         = true,  // Verify the "aud" claim matches our audience
            ValidateLifetime         = true,  // Reject expired tokens
            ValidateIssuerSigningKey = true,  // Verify the signature with our secret key
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────────────────────────
// Middleware pipeline
// ──────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AngularCorsPolicy");

// Authentication must come BEFORE Authorization in the pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
