using UnityEngine;

public class RoomComponent : Cell
{
    public RoomComponent(Vector3Int location, Vector3Int size) : base(location, size, CellType.RoomComponent)
    {
        
    }
}
