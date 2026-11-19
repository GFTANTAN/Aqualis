using AquaMaterials.Api.Models;
using AquaMaterials.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GoogleSheetOptions>(builder.Configuration.GetSection("GoogleSheet"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GoogleSheetService>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
