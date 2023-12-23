using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LOM.Levels;
using LOM.Files;
using System.Text;

namespace LOM.Spaces;

public partial class WorldSpace : Space {
    private static bool Debugging = true;
    private int id = 0;
    private string spaceName = "Overworld";
    private string basePath;
    private TileSetManager tileSetManager;

    public WorldSpace(string spaceName, TileSetManager tileSetManager){
        this.spaceName = spaceName;
        this.tileSetManager = tileSetManager;
        basePath = Main.world.GetSavePath() + "/" + spaceName;
        Directory.Ensure(basePath);
    }

    public Task<LevelCell> GetLevelCell(CellPosition coords){
        CompressedFile file = new(GetFullCellPathByCoords(coords), CompressedFile.ReadWriteMode.Read);
        if (!file.IsOpened()){
            return Task.Run(() => {
                return GenerateNewCell(coords);
            });
        }
        return Task.Run(() => {
            return LoadCellFromDisk(file, tileSetManager);
        });
    }

    public void StoreBytesToCell(byte[] cellToStore, CellPosition coords){
        CompressedFile file = new(GetFullCellPathByCoords(coords), CompressedFile.ReadWriteMode.Write);
        if (!file.IsOpened()){
            if (Debugging) 
            Debug.Print("WorldSpace: Attempt to write to " + GetFullCellPathByCoords(coords) + " failed.");
            return;
        }
        file.StoreInt64((ulong)cellToStore.Length);
        file.StoreBuffer(cellToStore);
    }

    /// <summary>
    /// Loads a given <c>LevelCell</c> from the given file.
    /// </summary>
    /// <param name="file">The file to be opened.</param>
    /// <returns>The LevelCell stored in the file.</returns>
    private static LevelCell LoadCellFromDisk(IFileRead file, TileSetManager tileSetManager){
        int bufferLength = (int)file.ReadInt64();
        byte[] buffer = file.ReadBuffer(bufferLength);
        file.Close();
        return LevelCell.Deserialize(buffer);
    }

    /// <summary>
    /// Generates a new <c>LevelCell</c> at the given coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> to generate in cell 
    /// space.</param>
    /// <returns>A newly generated <c>LevelCell</c> at the given coordinates.</returns>
    private LevelCell GenerateNewCell(CellPosition coords){
        int forestRefId = tileSetManager.GetTileSetCode("Forest");
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
                newCell.Place(0, new Position(i, j), fill);
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
		int buildingsRefId = tileSetManager.GetTileSetCode("Elf_Buildings");
		TileMapLoader loader = new(tileSetManager, 
        "res://Scenes/Maps/elf_buildings_test.tscn", 
        LevelCell.Width,
        LevelCell.Height,
        LevelCell.NumLayers);
        input.PlaceAll(0, new Position(0,0), loader.Load(buildingsRefId));
	}

    /// <summary>
    /// Gets the full file name and path for the <c>LevelCell</c> at the given coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> in question.</param>
    /// <returns>A string giving the path as specified above.</returns>
    private string GetFullCellPathByCoords(CellPosition coords){
        string cellPath = GetCellFileName(coords);
        return basePath + "/" + cellPath + ".dat";
    }

    /// <summary>
    /// Gets the file name for the given <c>LevelCell</c> at the specified coordinates.
    /// </summary>
    /// <param name="coords">The coordinates of the <c>LevelCell</c> in question.</param>
    /// <returns>A string representing the file name of the <c>LevelCell</c></returns>
    private static string GetCellFileName(CellPosition coords){
        return coords.X + "_" + coords.Y;
    }

    public byte[] Serialize(){
        return Encoding.ASCII.GetBytes(spaceName);
    }

    public static WorldSpace Deserialize(byte[] input, TileSetManager tileSetManager){
        string name = Encoding.ASCII.GetString(input);
        return new WorldSpace(name, tileSetManager);
    }

}