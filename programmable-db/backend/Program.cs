using ProgrammableDb.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DataSourceStore>();
builder.Services.AddSingleton<DbExecutionService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
