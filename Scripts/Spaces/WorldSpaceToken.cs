using System.Text;

namespace LOM.Spaces;

public class WorldSpaceToken : SpaceToken {
    public string spaceName;

    public WorldSpaceToken(string spaceName) : base(TokenType.WorldSpace){
        this.spaceName = spaceName;
    }

    public byte[] Serialize(){
        return Encoding.ASCII.GetBytes(spaceName);
    }

    public static WorldSpaceToken Deserialize(byte[] bytes){
        string readName = Encoding.ASCII.GetString(bytes);
        return new WorldSpaceToken(readName);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)){
            return false;
        }
        if (obj is not WorldSpaceToken){
            return false;
        }
        WorldSpaceToken other = (WorldSpaceToken)obj;
        return other.spaceName == spaceName;
    }

    public override int GetHashCode()
    {
        return spaceName.GetHashCode();
    }
}