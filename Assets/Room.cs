using UnityEngine;

public class Room : Cell
{
    public Room(Vector3Int location, Vector3Int size) : base(location, size, CellType.Room) {
        bounds = new BoundsInt(location, size);
    }

    public static bool Intersect(Room a, Room b) {
        return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
            || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y)
            || (a.bounds.position.z >= (b.bounds.position.z + b.bounds.size.z)) || ((a.bounds.position.z + a.bounds.size.z) <= b.bounds.position.z));
    }

}