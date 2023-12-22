using LOM.Spaces;

namespace LOM.Levels;

public abstract class LevelCellRequest {
    public enum RequestStatus {
        Open = 1,
        Fulfilled = 2,
        Error = 3
    }
    public RequestStatus requestStatus;
    public LevelCell payload;
    public SpaceToken requestSpace;
    public CellPosition coords;

    public LevelCellRequest(SpaceToken requestSpace, CellPosition coords) {
        this.requestSpace = requestSpace;
        this.coords = coords;
        requestStatus = RequestStatus.Open;
    }
}