using AIEventDiscovery.Configuration;
using AIEventDiscovery.Middleware;
using AIEventDiscovery.Services;
using AIEventDiscovery.Services.DataImport;
using AIEventDiscovery.Services.Embeddings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AIEventDiscovery.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IChromaService, ChromaService>();

// Register embedding service as a Singleton so the ONNX model stays loaded in memory
builder.Services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();

builder.Services.AddScoped<IDataImportService, DataImportService>();

builder.Services.Configure<ChromaDbOptions>(builder.Configuration.GetSection("ChromaDb"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
