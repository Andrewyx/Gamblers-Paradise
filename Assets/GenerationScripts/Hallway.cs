using UnityEngine;

public class Hallway : Cell
{
    public Hallway(Vector3Int location, Vector3Int size) : base(location, size, CellType.Hallway)
    {
        
    }
}