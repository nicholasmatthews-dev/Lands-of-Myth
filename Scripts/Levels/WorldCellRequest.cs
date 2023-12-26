using System;
using System.Collections.Generic;
using LOM.Spaces;

namespace LOM.Levels;

public class WorldCellRequest : LevelCellRequest {
    public static int MagicNumber = 5;
    private WorldSpaceToken worldSpace;
    public WorldCellRequest(WorldSpaceToken worldSpace, CellPosition coords) 
    : base(worldSpace, coords, RequestType.WorldCell){
        this.worldSpace = worldSpace;
    }

    public override byte[] Serialize(){
        byte[] header = base.Serialize();
        byte[] payload = GetBytes();
        byte[] output = new byte[header.Length + payload.Length];
        int byteHead = 0;
        SerializationHelper.AppendBytes(ref output, header, ref byteHead);
        SerializationHelper.AppendBytes(ref output, payload, ref byteHead);
        return output;
    }

    protected override byte[] GetBytes(){
        List<byte[]> items;
        if (payload is not null){
            items = new(){
                worldSpace.Serialize(),
                coords.Serialize(),
                payload.Serialize()
            };
        }
        else {
            items = new(){
                worldSpace.Serialize(),
                coords.Serialize()
            };
        }
        return SerializationHelper.Stitch(MagicNumber, items);
    }

    public static WorldCellRequest FromBytes(byte[] bytes){
        List<byte[]> items = SerializationHelper.Unstitch(MagicNumber, bytes);
        if (items.Count < 2 || items.Count > 3){
            throw new ArgumentException("WorldCellRequest: Unstitched list is " + items.Count + " elements long.");
        }
        WorldSpaceToken spaceToken = WorldSpaceToken.Deserialize(items[0]);
        Position coords = Position.Deserialize(items[1]);
        CellPosition cellCoords = new(coords.X, coords.Y);
        WorldCellRequest output = new(spaceToken, cellCoords);
        if (items.Count == 3){
            LevelCell loadedCell = LevelCell.Deserialize(items[2]);
            output.payload = loadedCell;
        }
        return output;
    }

    public override bool Equals(object obj)
    {
        bool baseEquals = base.Equals(obj);
        if (!baseEquals){
            return false;
        }
        if (obj is not WorldCellRequest){
            return false;
        }
        WorldCellRequest other = (WorldCellRequest)obj;
        return other.worldSpace == worldSpace;
    }

    public override int GetHashCode()
    {
        return (coords, worldSpace).GetHashCode();
    }

    public override string ToString()
    {
        if (payload is not null){
            return "(WorldCellRequest: SpaceName: " + worldSpace.spaceName 
            + "; Coords: " + coords 
            + "; Status: " + requestStatus
            + "; Payload: " + payload
            + ")";
        }
        else {
            return "(WorldCellRequest: SpaceName: " + worldSpace.spaceName 
            + "; Coords: " + coords 
            + "; Status: " + requestStatus
            + ")";
        }
        
    }

}