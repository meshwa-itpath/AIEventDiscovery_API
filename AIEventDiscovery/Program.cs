using System.Text;
using AIEventDiscovery.Configuration;
using AIEventDiscovery.Data.Repositories;
using AIEventDiscovery.Middleware;
using AIEventDiscovery.Services;
using AIEventDiscovery.Services.DataImport;
using AIEventDiscovery.Services.Embeddings;
using AIEventDiscovery.Services.Interfaces;
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

    // Apply the scheme globally so every endpoint shows the lock icon
    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id   = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});
});

// PostgreSQL via EF Core
builder.Services.AddDbContext<AIEventDiscovery.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ChromaDB HTTP client
builder.Services.AddHttpClient<IChromaService, ChromaService>();

// Gemini HTTP client
builder.Services.AddHttpClient<IGeminiService, AIEventDiscovery.Services.LLM.GeminiService>();

// Local embedding model (ONNX, loaded once at startup as a singleton)
builder.Services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();

// Data import and authentication services
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRetrievalService, AIEventDiscovery.Services.RAG.RetrievalService>();

// Generic repository — registered as an open generic so any IGenericRepository<TEntity>
// can be injected without registering each entity separately
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Options binding
builder.Services.Configure<ChromaDbOptions>(builder.Configuration.GetSection("ChromaDb"));
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
