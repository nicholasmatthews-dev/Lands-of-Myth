using Godot;
using System;
using System.Diagnostics;

public class WorldSpace : Space {
    private int id = 0;
    private string spaceName = "Overworld";
    private string basePath;

    public WorldSpace() : base(){
        basePath = Main.world.GetSavePath() + "/" + spaceName;
        Debug.Print("WorldSpace: Attempting to create directory: \"" + basePath + "\"");
        Error error = DirAccess.MakeDirRecursiveAbsolute(basePath);
        Debug.Print("WorldSpace: Error is " + error);
    }

    public override LevelCell GetLevelCell(Vector2I coords){
        using var file = FileAccess.Open(GetFullCellPathByCoords(coords), FileAccess.ModeFlags.Read);
        if (file is null){
            return GenerateNewCell(coords);
        }
        return LoadCellFromDisk(file);
    }

    public override void StoreBytesToCell(byte[] cellToStore, Vector2I coords){
        Debug.Print("WorldSpace: Saving cell to \"" + GetFullCellPathByCoords(coords) + "\"");
        using var file = FileAccess.Open(GetFullCellPathByCoords(coords), 
        FileAccess.ModeFlags.Write);
        if (file is null){
            Debug.Print("WorldSpace: Attempt to write failed.");
            Debug.Print("WorldSpace: Error: " + FileAccess.GetOpenError());
            return;
        }
        file.Store64((ulong)cellToStore.Length);
        file.StoreBuffer(cellToStore);
    }

    private static LevelCell LoadCellFromDisk(FileAccess file){
        Debug.Print("WorldSpace: Loading cell from \"" + file.GetPath() + "\".");
        int bufferLength = (int)file.Get64();
        byte[] buffer = file.GetBuffer(bufferLength);
        return LevelCell.Deserialize(buffer);
    }

    private LevelCell GenerateNewCell(Vector2I coords){
        Debug.Print("WorldSpace: Generating new cell with coords: " + coords);
        int forestRefId = Main.tileSetManager.GetTileSetCode("Forest");
        LevelCell newCell = new LevelCell();
        Vector2I fill;
        if ((coords.X + coords.Y) % 2 == 0){
            fill = new Vector2I(0,0);
        }
        else{
            fill = new Vector2I(2,1);
        }
        for (int i = 0; i < LevelCell.Width; i++){
            for (int j = 0; j < LevelCell.Height; j++){
                newCell.Place(0, forestRefId, new Vector2I(i,j), fill);
            }
        }
        if (coords.X == 0 && coords.Y == 0){
            AddStructure(newCell);
        }
        return newCell;
    }

    private void AddStructure(LevelCell input){
		int buildingsRefId = Main.tileSetManager.GetTileSetCode("Elf_Buildings");
		TileMap houses = (TileMap)ResourceLoader
		.Load<PackedScene>("res://Scenes/Maps/elf_buildings_test.tscn")
		.Instantiate();
		for (int i = 0; i < LevelCell.Width; i++){
			for (int j = 0; j < LevelCell.Height; j++){
				for (int k = 0; k < 2; k++){
					Vector2I atlasCoords = houses.GetCellAtlasCoords(k, new Vector2I(i, j));
					input.Place(k + 1, buildingsRefId, new Vector2I(i, j), atlasCoords);
				}
			}
		}
		houses.Free();
	}

    private string GetFullCellPathByCoords(Vector2I coords){
        string cellPath = GetCellPathByCoords(coords);
        return basePath + "/" + cellPath + ".dat";
    }

    private static string GetCellPathByCoords(Vector2I coords){
        return coords.X + "_" + coords.Y;
    }

}