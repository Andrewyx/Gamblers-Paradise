using System.Collections.Generic;
using UnityEngine;
public class Cell
{
    public bool PosX, PosZ, NegX, NegZ;

    public CellType cellType;
    public Vector3Int location;
    public BoundsInt bounds;
    public Cell(Vector3Int location, Vector3Int size, CellType cellType)
    {
        this.cellType = cellType;
        this.location = location;
        bounds = new BoundsInt(location, size);
        PosX = true;
        PosZ = true;
        NegX = true;
        NegZ = true;
    }
    
}
