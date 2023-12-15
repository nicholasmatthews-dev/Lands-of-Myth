using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LOM.Levels;

namespace LOM.Spaces;

public partial class WorldSpace : Space {
    private int id = 0;
    private string spaceName = "Overworld";
    private string basePath;

    public WorldSpace() : base(){
        basePath = Main.world.GetSavePath() + "/" + spaceName;
        Debug.Print("WorldSpace: Attempting to create directory: \"" + basePath + "\"");
        Error error = DirAccess.MakeDirRecursiveAbsolute(basePath);
        Debug.Print("WorldSpace: Error is " + error);
    }

    public override Task<LevelCell> GetLevelCell(Vector2I coords){
        FileAccess file = FileAccess.OpenCompressed
        (
            GetFullCellPathByCoords(coords), 
            FileAccess.ModeFlags.Read,
            compressionMode
        );
        if (file is null){
            return Task.Run(() => {
                return GenerateNewCell(coords);
            });
        }
        return Task.Run(() => {
            return LoadCellFromDisk(file);
        });
    }

    public override void StoreBytesToCell(byte[] cellToStore, Vector2I coords){
        using var file = FileAccess.OpenCompressed
        (
            GetFullCellPathByCoords(coords), 
            FileAccess.ModeFlags.Write,
            compressionMode
        );
        if (file is null){
            Debug.Print("WorldSpace: Attempt to write to " + GetFullCellPathByCoords(coords) + " failed.");
            Debug.Print("WorldSpace: Error: " + FileAccess.GetOpenError());
            return;
        }
        file.Store64((ulong)cellToStore.Length);
        file.StoreBuffer(cellToStore);
    }

    /// <summary>
    /// Loads a given <c>LevelCell</c> from the given file.
    /// </summary>
    /// <param name="file">The file to be opened.</param>
    /// <returns>The LevelCell stored in the file.</returns>
    private static LevelCell LoadCellFromDisk(FileAccess file){
        int bufferLength = (int)file.Get64();
        byte[] buffer = file.GetBuffer(bufferLength);
        file.Close();
        return LevelCell.Deserialize(buffer);
    }

    /// <summary>
    /// Generates a new <c>LevelCell</c> at the given coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> to generate in cell 
    /// space.</param>
    /// <returns>A newly generated <c>LevelCell</c> at the given coordinates.</returns>
    private LevelCell GenerateNewCell(Vector2I coords){
        int forestRefId = GameModel.tileSetManager.GetTileSetCode("Forest");
        LevelCell newCell = new LevelCell();
        Tile fill;
        if ((coords.X + coords.Y) % 2 == 0){
            fill = new(forestRefId, 0, 0);
        }
        else{
            fill = new(forestRefId, 2, 1);
        }
        for (int i = 0; i < LevelCell.Width; i++){
            for (int j = 0; j < LevelCell.Height; j++){
                newCell.Place(0, (i, j), fill);
            }
        }
        if (coords.X == 0 && coords.Y == 0){
            AddStructure(ref newCell);
        }
        return newCell;
    }

    /// <summary>
    /// A testing method which adds in the test structure to a given cell.
    /// </summary>
    /// <param name="input">The <c>LevelCell</c> to be modified.</param>
    private void AddStructure(ref LevelCell input){
		int buildingsRefId = GameModel.tileSetManager.GetTileSetCode("Elf_Buildings");
		TileMap houses = (TileMap)ResourceLoader
		.Load<PackedScene>("res://Scenes/Maps/elf_buildings_test.tscn")
		.Instantiate();
		for (int i = 0; i < LevelCell.Width; i++){
			for (int j = 0; j < LevelCell.Height; j++){
				for (int k = 0; k < 2; k++){
                    if (houses.GetCellSourceId(k, new Vector2I(i,j)) != -1){
                        Vector2I atlasCoords = houses.GetCellAtlasCoords(k, new Vector2I(i, j));
                        Tile toPlace = new(buildingsRefId, atlasCoords.X, atlasCoords.Y);
                        input.Place(k + 1, (i,j), toPlace);
                    }
				}
			}
		}
		houses.Free();
	}

    /// <summary>
    /// Gets the full file name and path for the <c>LevelCell</c> at the given coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> in question.</param>
    /// <returns>A string giving the path as specified above.</returns>
    private string GetFullCellPathByCoords(Vector2I coords){
        string cellPath = GetCellFileName(coords);
        return basePath + "/" + cellPath + ".dat";
    }

    /// <summary>
    /// Gets the file name for the given <c>LevelCell</c> at the specified coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> in question.</param>
    /// <returns>A string representing the file name of the <c>LevelCell</c></returns>
    private static string GetCellFileName(Vector2I coords){
        return coords.X + "_" + coords.Y;
    }

}