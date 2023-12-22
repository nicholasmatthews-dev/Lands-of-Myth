namespace LOM.Spaces;

public abstract class SpaceToken {
    public enum TokenType {
        WorldSpace = 1
    }

    public SpaceToken(TokenType type){
        tokenType = type;
    }

    public TokenType tokenType;

    public static bool operator ==(SpaceToken a, SpaceToken b) => a.Equals(b);
    public static bool operator !=(SpaceToken a, SpaceToken b) => !a.Equals(b);


    public override bool Equals(object obj)
    {
        if (obj is null){
            return false;
        }
        if (obj is not SpaceToken){
            return false;
        }
        SpaceToken other = (SpaceToken)obj;
        return other.tokenType == tokenType;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}