DECLARE @board AS char(256)
SELECT @board = board FROM [dbo].[MinesweeperGames] WHERE id = @id

UPDATE [dbo].[MinesweeperGames] SET oldChar = SUBSTRING(@board, @move, 1) WHERE id = @id

IF(ASCII(SUBSTRING(@board, @move, 1)) > 57)
BEGIN
SET @board = STUFF(@board, @move, 1, CHAR(ASCII(SUBSTRING(@board, @move, 1)) - 16))
UPDATE [dbo].[MinesweeperGames] SET board = @board WHERE id = @id
END

SELECT board, oldChar FROM [dbo].[MinesweeperGames] WHERE id = @id