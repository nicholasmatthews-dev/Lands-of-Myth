using System;
using System.Diagnostics;
using LOM.Spaces;

namespace LOM.Levels;

public abstract class LevelCellRequest {
    private static bool Debugging = false;
    private static int MagicNumber = 3;
    public enum RequestType {
        WorldCell = 1
    }

    public enum RequestStatus {
        Open = 1,
        Fulfilled = 2,
        Error = 3
    }
    public RequestType requestType;
    public RequestStatus requestStatus;
    public LevelCell payload;
    public SpaceToken requestSpace;
    public CellPosition coords;

    public static bool operator ==(LevelCellRequest a, LevelCellRequest b) => a.Equals(b);
    public static bool operator !=(LevelCellRequest a, LevelCellRequest b) => !a.Equals(b);

    public virtual byte[] Serialize(){
        byte[] header = new byte[6];
        int byteHead = 0;
        SerializationHelper.StoreInt(ref header, MagicNumber, ref byteHead);
        header[byteHead] = (byte)requestType;
        byteHead++;
        header[byteHead] = (byte)requestStatus;
        byteHead++;
        return header;
    }

    protected abstract byte[] GetBytes();

    public static LevelCellRequest Deserialize(byte[] bytes){
        int byteHead = 0;
        int readMagicNumber = SerializationHelper.ReadInt(bytes, ref byteHead);
        if (readMagicNumber != MagicNumber){
            throw new Exception("LevelCellRequest: Magic number mismatch " + readMagicNumber + " does not equal " 
            + MagicNumber);
        }
        RequestType readRequestType = (RequestType)bytes[byteHead];
        byteHead++;
        RequestStatus readRequestStatus = (RequestStatus)bytes[byteHead];
        byteHead++;
        if (Debugging) Debug.Print("LevelCellRequest: About to call FromBytes, byteHead is " + byteHead 
        + " and bytes.Length is " + bytes.Length);
        byte[] remaining = SerializationHelper.ReadBytes(bytes, bytes.Length - byteHead, ref byteHead);
        if (readRequestType == RequestType.WorldCell){
            return WorldCellRequest.FromBytes(remaining);
        }
        else {
            throw new Exception("LevelCellRequest: No matching request type found.");
        }
    }

    public LevelCellRequest(SpaceToken requestSpace, CellPosition coords, RequestType requestType) {
        this.requestType = requestType;
        this.requestSpace = requestSpace;
        this.coords = coords;
        requestStatus = RequestStatus.Open;
    }

    public override bool Equals(object obj)
    {
        if (obj is null){
            return false;
        }
        if (obj is not LevelCellRequest){
            return false;
        }
        LevelCellRequest other = (LevelCellRequest)obj;
        bool equal = true;
        equal = equal && (other.coords == coords);
        equal = equal && (other.requestType == requestType);
        equal = equal && (other.requestSpace == requestSpace);
        return equal;
    }

    public override int GetHashCode()
    {
        return (coords, requestType, requestStatus, requestSpace).GetHashCode();
    }
}