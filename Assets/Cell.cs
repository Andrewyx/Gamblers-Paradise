using System.Collections.Generic;
using UnityEngine;
public class Cell
{
    public bool North, East, South, West;

    public List<bool> walls;
    public CellType cellType;
    public Vector3Int location;
    public Vector3Int size;
    public Cell(Vector3Int location, Vector3Int size, CellType cellType)
    {
        this.cellType = cellType;
        this.location = location;
        this.size = size;
        North = true;
        East = true;
        West = true;
        South = true;
        walls = new List<bool> {North, East, South, West};
    }
}
