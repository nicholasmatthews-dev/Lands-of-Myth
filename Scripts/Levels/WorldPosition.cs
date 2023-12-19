namespace LOM.Levels;

/// <summary>
/// Represents a position in world space.
/// </summary>
public class WorldPosition : Position {
    public WorldPosition(int X, int Y) : base(X, Y){
        
    }

    /// <summary>
    /// Gets a tuple which represents the position of the cell (in cell space) and the local position
    /// that this <see cref="WorldPosition"/> represents.
    /// </summary>
    /// <param name="cellWidth">The width of a cell (in tiles)</param>
    /// <param name="cellHeight">The height of a cell (in tiles)</param>
    /// <returns></returns>
    public (CellPosition, Position) GetCellCoords(int cellWidth, int cellHeight){
        int cellX = FloorDivision(posX, cellWidth);
        int cellY = FloorDivision(posY, cellHeight);

        int localX = posX % cellWidth;
        if (localX < 0){
            localX += cellWidth;
        }
        int localY = posY % cellHeight;
        if (localY < 0){
            localY += cellHeight;
        }

        return (new CellPosition(cellX, cellY), new Position(localX, localY));
    }

    private static int FloorDivision(int a, int b){
		if (((a < 0) || (b < 0)) && (a % b != 0)){
			return (a / b - 1);
		}
		else {
			return (a / b);
		}
	}
}