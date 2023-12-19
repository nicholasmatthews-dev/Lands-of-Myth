namespace LOM.Levels;

/// <summary>
/// Represents a two dimensional position.
/// </summary>
public class Position {
    protected int posX = 0;
    protected int posY = 0;

    public int X {get => posX;}
    public int Y {get => posY;}

    public Position(int X, int Y){
        posX = X;
        posY = Y;
    }

    public static Position operator +(Position a, Position b) => new(a.posX + b.posX, a.posY + b.posY);
    public static Position operator -(Position a, Position b) => new(a.posX - b.posX, a.posY - b.posY);

    public override bool Equals(object obj)
    {
        if (obj.GetType() != GetType()){
            return false;
        }
        Position other = (Position)obj;
        return other.posX == posX && other.posY == posY;
    }

    public override int GetHashCode()
    {
        return (posX, posY).GetHashCode();
    }
}