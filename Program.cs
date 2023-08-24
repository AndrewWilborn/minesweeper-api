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

try
{
    // Table would be created ahead of time in production
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "CREATE TABLE Persons (ID int NOT NULL PRIMARY KEY IDENTITY, FirstName varchar(255), LastName varchar(255));",
        conn);
    using SqlDataReader reader = command.ExecuteReader();
}
catch (Exception e)
{
    // Table may already exist
    Console.WriteLine(e.Message);
}

app.MapGet("/Person", () => {
    var rows = new List<string>();

    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand("SELECT * FROM Persons", conn);
    using SqlDataReader reader = command.ExecuteReader();

    if (reader.HasRows)
    {
        while (reader.Read())
        {
            rows.Add($"{reader.GetInt32(0)}, {reader.GetString(1)}, {reader.GetString(2)}");
        }
    }

    return rows;
})
.WithName("GetPersons");

app.MapPost("/Person", (Person person) => {
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "INSERT INTO Persons (firstName, lastName) VALUES (@firstName, @lastName)",
        conn);

    command.Parameters.Clear();
    command.Parameters.AddWithValue("@firstName", person.FirstName);
    command.Parameters.AddWithValue("@lastName", person.LastName);

    using SqlDataReader reader = command.ExecuteReader();
})
.WithName("CreatePerson");

app.Run();

public class Person
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}