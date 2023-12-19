using Godot;

namespace LOM.Levels;

public class TileMapLoader {
    private TileSetManager tileSetManager;
    private string path;
    private int cellWidth;
    private int cellHeight;
    private int numLayers;
    public TileMapLoader(TileSetManager tileSetManager, string path, int cellWidth, int cellHeight, int numLayers){
        this.tileSetManager = tileSetManager;
        this.path = path;
        this.cellWidth = cellWidth;
        this.cellHeight = cellHeight;
        this.numLayers = numLayers;
    }

    public Tile[,,] Load(int tileSetRefId){
        Tile[,,] tiles = new Tile[cellWidth, cellHeight, numLayers];
        TileMap tileMap = (TileMap)ResourceLoader
		.Load<PackedScene>(path)
		.Instantiate();
		for (int i = 0; i < cellWidth; i++){
			for (int j = 0; j < cellHeight; j++){
				for (int k = 0; k < numLayers; k++){
                    if (tileMap.GetCellSourceId(k, new Vector2I(i,j)) != -1){
                        Vector2I atlasCoords = tileMap.GetCellAtlasCoords(k, new Vector2I(i, j));
                        Tile toPlace = new(tileSetRefId, atlasCoords.X, atlasCoords.Y);
                        tiles[i,j,k] = toPlace;
                    }
				}
			}
		}
        return tiles;
    }
}