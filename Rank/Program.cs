using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Service.Services;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Doc", Version = "v1" });
 
});
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();
builder.Services.AddSingleton<ISkipCaseService, SkipCaseService>();
var app = builder.Build();

app.UseSwaggerUI(options =>
{
    options.DisplayOperationId();
    options.ShowExtensions();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();