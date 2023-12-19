using System;
using System.Text;
using LOM.Levels;

namespace LOM.Spaces;

public class WorldCellRequest {
    public enum RequestStatus {
        Open = 1,
        Fulfilled = 2,
        Error = 3
    }
    public static byte MagicNumber = 4;

    public string spaceName;
    public CellPosition coords;
    public RequestStatus status;
    public byte[] payload;

    public WorldCellRequest(string spaceName, CellPosition coords){
        this.spaceName = spaceName;
        this.coords = coords;
        status = RequestStatus.Open;
    }

    public byte[] Serialize(){
        byte[] spaceNameAsBytes = Encoding.ASCII.GetBytes(spaceName);
        byte[] output;
        int totalSizeInBytes;
        int byteHead = 0;
        if (payload is not null){
            totalSizeInBytes = 1 + 1 + 4 + spaceNameAsBytes.Length + 4 + 4 + 4 + payload.Length;
        }
        else {
            totalSizeInBytes = 1 + 1 + 4 + spaceNameAsBytes.Length + 4 + 4;
        }
        output = new byte[totalSizeInBytes];
        SerializationHelper.AppendBytes(ref output, new byte[]{MagicNumber}, ref byteHead);
        SerializationHelper.AppendBytes(ref output, new byte[]{(byte)status}, ref byteHead);
        SerializationHelper.StoreString(ref output, spaceName, ref byteHead);
        SerializationHelper.StoreInt(ref output, coords.X, ref byteHead);
        SerializationHelper.StoreInt(ref output, coords.Y, ref byteHead);
        if (payload is not null){
            SerializationHelper.StoreInt(ref output, payload.Length, ref byteHead);
            SerializationHelper.AppendBytes(ref output, payload, ref byteHead);
        }
        return output;
    }

    public static WorldCellRequest Deserialize(byte[] input){
        if (input[0] != MagicNumber){
            throw new ArgumentException("WorldCellRequest: Magic number mismatch " + input[0] + ", invalid request.");
        }
        int byteHead = 1;
        RequestStatus status = (RequestStatus)SerializationHelper.ReadBytes(input, 1, ref byteHead)[0];
        string spaceName = SerializationHelper.ReadString(input, ref byteHead);
        int coordsX = SerializationHelper.ReadInt(input, ref byteHead);
        int coordsY = SerializationHelper.ReadInt(input, ref byteHead);
        CellPosition coords = new(coordsX, coordsY);
        if (byteHead >= input.Length){
            return new WorldCellRequest(spaceName, coords){
                status = status
            };
        }
        else {
            WorldCellRequest request = new(spaceName, coords){
                status = status
            };
            int payloadLength = SerializationHelper.ReadInt(input, ref byteHead);
            byte[] payload = SerializationHelper.ReadBytes(input, payloadLength, ref byteHead);
            request.payload = payload;
            return request;
        }
    }

    public override string ToString()
    {
        if (payload is null){
            return "(WorldCellRequest: Name = " + spaceName + "; Coords = " + coords + "; Status = " + status + ")";
        }
        else {
            return "(WorldCellRequest: Name = " + spaceName 
            + "; Coords = " + coords 
            + "; Status = " + status 
            + "; PayloadLength = " + payload.Length + ")";
        }
    }
}