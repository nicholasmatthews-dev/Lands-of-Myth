using System;
using System.Diagnostics;

namespace LOM.Levels;

/// <summary>
/// Represents a two dimensional position.
/// </summary>
public class Position {
    private static bool Debugging = false;
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
    public static bool operator ==(Position a, Position b) => a.Equals(b);
    public static bool operator !=(Position a, Position b) => !a.Equals(b);

    public override bool Equals(object obj)
    {
        if (obj.GetType() != GetType()){
            if (Debugging) Debug.Print(GetType() + ": Other " + obj.GetType() + " is not the same type.");
            return false;
        }
        Position other = (Position)obj;
        return other.X == posX && other.Y == posY;
    }

    public byte[] Serialize(){
        byte[] output = new byte[8];
        int byteHead = 0;
        SerializationHelper.StoreInt(ref output, X, ref byteHead);
        SerializationHelper.StoreInt(ref output, Y, ref byteHead);
        return output;
    }

    public static Position Deserialize(byte[] bytes){
        if (bytes.Length != 8){
            throw new ArgumentException("Position: Error in deserializing postion " 
            + bytes.Length 
            + " is not 8 bytes.");
        }
        int byteHead = 0;
        int readX = SerializationHelper.ReadInt(bytes, ref byteHead);
        int readY = SerializationHelper.ReadInt(bytes, ref byteHead);
        return new Position(readX, readY);
    }

    public override int GetHashCode()
    {
        return (posX, posY).GetHashCode();
    }

    public override string ToString()
    {
        return "(" + posX + ", " + posY + ")";
    }
}