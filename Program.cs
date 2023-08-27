using Microsoft.OpenApi.Models;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minesweeper API", Description = "An ASP.NET API built with Microsoft SQL Server for running minesweeper", Version = "v1" });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minesweeper API V1");
});

app.UseHttpsRedirection();

string connectionString = app.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")!;

// string tableQuery = "CREATE TABLE MinesweeperGames (id uniqueidentifier NOT NULL, board char(256) NOT NULL, timestart bigint, timeend bigint, completed bit);";

int[] adjacentLocations = { -17, -16, -15, -1, 1, 15, 16, 17 };
int[] leftExceptions = {-17, -1, 15};
int[] rightExceptions = {-15, 1, 17};
int GetAdjacentMines(int index, char[] board)
{
    int returnVal = 0;
    for (int i = 0; i < adjacentLocations.Length; i++)
    {
        if (index % 16 == 0 && leftExceptions.Contains(adjacentLocations[i])) continue;
        if (index % 16 == 15 && rightExceptions.Contains(adjacentLocations[i])) continue;
        int adjacentIndex = index + adjacentLocations[i];
        if (adjacentIndex >= 0 && adjacentIndex < board.Length)
        {
            if (board[adjacentIndex] == ':') returnVal++;
        }
    }
    return returnVal;
}

string GetNewBoard(int firstMove)
{
    // Generate board with 50 mines
    char[] board;
    if (firstMove > 128) board = (new string(':', 50) + new string('@', 206)).ToCharArray();
    else board = (new string('@', 206) + new string(':', 50)).ToCharArray();

    // Randomize locations of the mines
    int currentIndex = board.Length, randomIndex;
    char temp;
    Random rnd = new();
    List<int> protectedIndexes = new List<int> { firstMove };
    for (int i = 0; i < adjacentLocations.Length; i++) protectedIndexes.Add(adjacentLocations[i] + firstMove);
    while (currentIndex != 0)
    {
        randomIndex = rnd.Next(currentIndex);
        currentIndex--;
        if (protectedIndexes.Contains(randomIndex)) continue;
        if (protectedIndexes.Contains(currentIndex)) continue;

        temp = board[currentIndex];
        board[currentIndex] = board[randomIndex];
        board[randomIndex] = temp;
    }

    // Add numbers for the amount of ajacent mines
    for (int i = 0; i < board.Length; i++)
    {
        if (board[i] == ':') continue;
        int asciiVal = board[i] + GetAdjacentMines(i, board);
        board[i] = (char)asciiVal;
    }

    return new string(board);
}

static long GetMillisecondsSinceEpoch(DateTime dateTime)
{
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    TimeSpan timeSpan = dateTime - epoch;
    return (long)timeSpan.TotalMilliseconds;
}

app.MapPost("/newGame", (int firstMove) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    Guid uuid = Guid.NewGuid();

    var command = new SqlCommand(
        "INSERT INTO MinesweeperGames (id, board, timestart) VALUES (@id, @board, @timestart)", conn
    );
    command.Parameters.Clear();
    command.Parameters.AddWithValue("@id", uuid);
    command.Parameters.AddWithValue("@board", GetNewBoard(firstMove));
    command.Parameters.AddWithValue("@timestart", GetMillisecondsSinceEpoch(DateTime.UtcNow));

    using SqlDataReader reader = command.ExecuteReader();

    return uuid;
})
.WithName("New Game");

app.MapPost("/move", (int move, Guid uuid) => {
    // one big sql query that returns the board after the move has been made
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand("DECLARE @board AS char(256); SELECT @board = board FROM [dbo].[MinesweeperGames] WHERE id = @id; IF(ASCII(SUBSTRING(@board, @move, 1)) > 63) BEGIN SET @board = STUFF(@board, @move, 1, CHAR(ASCII(SUBSTRING(@board, @move, 1)) - 16)); END UPDATE [dbo].[MinesweeperGames] SET board = @board WHERE id = @id; SELECT board from [dbo].[MinesweeperGames] WHERE id = @id;", conn);
    command.Parameters.Clear();
    command.Parameters.AddWithValue("@move", move);
    command.Parameters.AddWithValue("@id", uuid);
    using SqlDataReader reader = command.ExecuteReader();

    if(!reader.HasRows)
    {
        return "";
    }

    reader.Read();
    return reader.GetString(0);
})
.WithName("Move");

app.MapGet("/boardDev", (Guid uuid) =>
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "SELECT board FROM MinesweeperGames WHERE id = @id", conn
    );
    command.Parameters.Clear();
    command.Parameters.AddWithValue("@id", uuid);

    using SqlDataReader reader = command.ExecuteReader();

    string[] board = new string[16];
    for(int i = 0; i < 16; i++)
    {
        board[i] = "";
    }

    if (!reader.HasRows)
    {
        return board;
    }
    
    reader.Read();
    char[] rawBoard = reader.GetString(0).ToCharArray();
    for (int i = 0; i < rawBoard.Length; i++)
    {
        board[i/16] += rawBoard[i];
    }

    return board;
})
.WithName("Dev Get Board");

app.Run();