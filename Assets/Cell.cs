using System.Collections.Generic;
using UnityEngine;
public class Cell
{
    public bool PosX, PosZ, NegX, NegZ;

    public CellType cellType;
    public Vector3Int location;
    public Vector3Int size;
    public Cell(Vector3Int location, Vector3Int size, CellType cellType)
    {
        this.cellType = cellType;
        this.location = location;
        this.size = size;
        PosX = true;
        PosZ = true;
        NegX = true;
        NegZ = true;
    }
    
    // public void RenderWalls()
    // {
    //     if (PosX) RenderPosXWall();
    //     // if (NegX) ;
    //     // if (PosZ) ;
    //     // if (NegZ) ;
    // }
    // private void RenderPosXWall()
    // {
    //     
    // }
}
