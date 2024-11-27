using UnityEngine;
public class Cell
{
    public CellType cellType;
    public Vector3Int location;
    public Vector3Int size;
    public Cell(Vector3Int location, Vector3Int size, CellType cellType)
    {
        this.cellType = cellType;
        this.location = location;
        this.size = size;
    }
}
