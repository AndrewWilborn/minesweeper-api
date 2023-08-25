using Microsoft.OpenApi.Models;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minesweeper API", Description = "An ASP.NET API built with Microsoft SQL Server for running minesweeper", Version = "v1"});
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => {
  c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minesweeper API V1");
});

app.UseHttpsRedirection();

string connectionString = app.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")!;

// string tableQuery = "CREATE TABLE MinesweeperGames (id uniqueidentifier NOT NULL, board char(256) NOT NULL, timestart bigint, timeend bigint, completed bit);";



app.Run();
