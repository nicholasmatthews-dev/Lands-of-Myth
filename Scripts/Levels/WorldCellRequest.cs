using System;
using System.Collections.Generic;
using LOM.Spaces;

namespace LOM.Levels;

public class WorldCellRequest : LevelCellRequest {
    public static int MagicNumber = 5;
    private WorldSpaceToken worldSpace;
    public WorldCellRequest(WorldSpaceToken worldSpace, CellPosition coords) : base(worldSpace, coords){
        this.worldSpace = worldSpace;
    }

    public byte[] Serialize(){
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

    public static WorldCellRequest Deserialize(byte[] bytes){
        List<byte[]> items = SerializationHelper.Unstitch(MagicNumber, bytes);
        if (items.Count < 2 || items.Count > 3){
            throw new ArgumentException("WorldCellRequest: Unstitched list is " + items.Count + " elements long.");
        }
        WorldSpaceToken spaceToken = WorldSpaceToken.Deserialize(items[0]);
        CellPosition cellCoords = (CellPosition)CellPosition.Deserialize(items[1]);
        if (items.Count == 3){
            LevelCell loadedCell = LevelCell.Deserialize()
        }
    }

}