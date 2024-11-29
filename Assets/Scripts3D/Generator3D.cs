using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator3D : MonoBehaviour {

    [SerializeField]
    Vector3Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector3Int roomMaxSize;
    [SerializeField]
    GameObject cubePrefab;

    [SerializeField] private Mesh roomFloorMesh;
    [SerializeField] private GameObject stairPrefab;
    [SerializeField] private GameObject tallStairPrefab;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject roofPrefab;
    [SerializeField] private GameObject floorPrefab;
    
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;
    [SerializeField]
    Material greenMaterial;

    Random random;
    Grid3D<Cell> grid;
    List<Room> rooms;
    Delaunay3D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    void Start() {
        random = new Random(0);
        grid = new Grid3D<Cell>(size, Vector3Int.zero);
        rooms = new List<Room>();

        for (int i = 0; i < grid.Length(); i++)
        {
            grid[i] = new Cell(new Vector3Int(), new Vector3Int(), CellType.None);
        }
        
        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
        PlaceWalls();
    }

    void PlaceRooms()
    {
        bool startPlaced = false;
        for (int i = 0; i < roomCount; i++) {
            Vector3Int location = new Vector3Int(
                random.Next(0, size.x),
                random.Next(0, size.y),
                random.Next(0, size.z)
            );

            Vector3Int roomSize = new Vector3Int(
                random.Next(1, roomMaxSize.x + 1),
                random.Next(1, roomMaxSize.y + 1),
                random.Next(1, roomMaxSize.z + 1)
            );

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector3Int(-1, 0, -1), roomSize + new Vector3Int(2, 0, 2));

            // Room newRoom = new Room(location + Vector3Int.up*(roomSize.y/2), roomSize);
            // Room buffer = new Room(location + new Vector3Int(-1, 0, -1) + Vector3Int.up*(roomSize.y/2), roomSize + new Vector3Int(2, 0, 2));
            
            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y
                || newRoom.bounds.zMin < 0 || newRoom.bounds.zMax >= size.z) {
                add = false;
            }

            if (add && !startPlaced)
            {
                PlaceStartRoom(newRoom.bounds.position,newRoom.bounds.size);
                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    if (pos.y >= newRoom.bounds.center.y)
                        grid[pos] = newRoom;
                }
                rooms.Add(newRoom);
                startPlaced = true;
            }
            else if (add) {
                rooms.Add(newRoom);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);
                // DrawBox(newRoom.bounds.position + Vector3.up * 0.5f, Quaternion.identity, newRoom.bounds.size, Color.green);
                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
                    DrawBox(newRoom.bounds.center + Vector3.up * 0.5f, Quaternion.identity, new Vector3(1, 1, 1), Color.yellow);
                    grid[pos] = new Room(pos, new Vector3Int(1, 1, 1));
                }
            }
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            // vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
            vertices.Add(new Vertex<Room>(new Vector3(room.bounds.position.x, room.bounds.yMin, room.bounds.position.z), room));
        }
        
        delaunay = Delaunay3D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();
        
        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        DungeonPathfinder3D aStar = new DungeonPathfinder3D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = new Vector3(startRoom.bounds.center.x, startRoom.bounds.yMin, startRoom.bounds.center.z);
            var endPosf =  new Vector3(endRoom.bounds.center.x, endRoom.bounds.yMin, endRoom.bounds.center.z);
            var startPos = new Vector3Int((int)startPosf.x, (int)startPosf.y, (int)startPosf.z);
            var endPos = new Vector3Int((int)endPosf.x, (int)endPosf.y, (int)endPosf.z);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) => {
                var pathCost = new DungeonPathfinder3D.PathCost();

                var delta = b.Position - a.Position;

                if (delta.y == 0) {
                    //flat hallway
                    pathCost.cost = Vector3Int.Distance(b.Position, endPos);    //heuristic
                    if (grid[b.Position].cellType == CellType.Stairs) {
                            return pathCost;
                    } else if (grid[b.Position].cellType == CellType.Room) {
                        pathCost.cost += 5;
                    } else if (grid.HasItemAt(b.Position)) {
                        pathCost.cost += 1;
                    }
                    pathCost.traversable = true;
                } else {
                    //staircase
                    if ((grid[a.Position].cellType != CellType.None && grid[a.Position].cellType != CellType.Hallway)
                        || (grid[b.Position].cellType != CellType.None && grid[b.Position].cellType != CellType.Hallway)) return pathCost;

                    pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos);    //base cost + heuristic

                    int xDir = Mathf.Clamp(delta.x, -1, 1);
                    int zDir = Mathf.Clamp(delta.z, -1, 1);
                    Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                    Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                    if (!grid.InBounds(a.Position + verticalOffset)
                        || !grid.InBounds(a.Position + horizontalOffset)
                        || !grid.InBounds(a.Position + verticalOffset + horizontalOffset)) {
                        return pathCost;
                    }

                    if (grid[a.Position + horizontalOffset].cellType != CellType.None
                        || grid[a.Position + horizontalOffset * 2].cellType != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset].cellType != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset * 2].cellType != CellType.None) {
                        return pathCost;
                    }

                    pathCost.traversable = true;
                    pathCost.isStairs = true;
                }

                return pathCost;
            });

            if (path != null) {
                for (int i = 0; i < path.Count; i++) {
                    var current = path[i];

                    if (grid[current].cellType == CellType.None) {
                        grid[current] = new Hallway(current, new Vector3Int(1, 1, 1));
                    }

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                        int xDir = Mathf.Clamp(delta.x, -1, 1);
                        int zDir = Mathf.Clamp(delta.z, -1, 1);
                        Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                        Vector3 rot = new Vector3();
                        Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);
                        if (delta.y != 0)
                        {
                            grid[prev + horizontalOffset] =  new Stair(prev + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + horizontalOffset * 2] = new Stair(prev + horizontalOffset * 2, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset] = new Stair(prev + verticalOffset + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset * 2] = new Stair(prev + verticalOffset + horizontalOffset * 2, new Vector3Int(1, 1, 1));
                        }
                        if (delta.y > 0)
                        {
                            if (delta.x > 0)
                            {
                                rot.y = -90;
                            }
                            else if (delta.x < 0)
                            {
                                rot.y = 90;
                            }
                        
                            if (delta.z > 0)
                            {
                                rot.y = 180;
                            }
                            else if (delta.z < 0)
                            {
                                rot.y = 0;
                            }
                            
                            PlaceStairs(prev + horizontalOffset, prev + horizontalOffset * 2, rot);
                        }
                        else if (delta.y < 0)
                        {
                            if (delta.x > 0)
                            {
                                rot.y = 90;
                            }
                            else if (delta.x < 0)
                            {
                                rot.y = -90;
                            }
                        
                            if (delta.z > 0)
                            {
                                rot.y = 0;
                            }
                            else if (delta.z < 0)
                            {
                                rot.y = 180;
                            }
                            PlaceStairs(prev + verticalOffset + horizontalOffset * 2, prev + verticalOffset + horizontalOffset, rot);
                        }
                        Debug.DrawLine(prev, current, Color.blue, 1000, true);
                    }
                }

                foreach (var pos in path) {
                    if (grid[pos].cellType == CellType.Hallway) {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }

    void PlaceStartRoom(Vector3Int locationInt, Vector3Int size)
    {
        PlaceRoom(locationInt, size);
        GameObject p = Instantiate(playerPrefab, locationInt + new Vector3((size.x - 1) / 2f, Mathf.Floor(size.y/2f), (size.z - 1) / 2f) + Vector3.up/4, Quaternion.identity);
        p.GetComponent<Transform>().localScale = p.GetComponent<Transform>().localScale/5;
    }
    void PlaceRoom(Vector3Int locationInt, Vector3Int size) {
        int floorCountX = Mathf.Max(1, size.x);
        int floorCountZ = Mathf.Max(1, size.z);
        
        for (int x = 0; x < floorCountX; x++)
        {
            for (int z = 0; z < floorCountZ; z++)
            {
                Vector3Int location = locationInt + new Vector3Int(x, 0, z);
                Instantiate(floorPrefab, location, Quaternion.identity);
            }
        }
        
    }

    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);

        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

        Debug.DrawLine(point1, point2, c, 1000);
        Debug.DrawLine(point2, point3, c, 1000);
        Debug.DrawLine(point3, point4, c, 1000);
        Debug.DrawLine(point4, point1, c, 1000);

        Debug.DrawLine(point5, point6, c, 1000);
        Debug.DrawLine(point6, point7, c, 1000);
        Debug.DrawLine(point7, point8, c, 1000);
        Debug.DrawLine(point8, point5, c, 1000);

        Debug.DrawLine(point1, point5, c, 1000);
        Debug.DrawLine(point2, point6, c, 1000);
        Debug.DrawLine(point3, point7, c, 1000);
        Debug.DrawLine(point4, point8, c, 1000);
    }
    
    void PlaceHallway(Vector3Int location) {
        GameObject go = Instantiate(cubePrefab, location, Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3Int(1, 1, 1);
    }

    void PlaceStairs(Vector3Int location1, Vector3Int location2, Vector3 rotation) {
        GameObject go1 = Instantiate(stairPrefab, location1, Quaternion.identity);
        Transform transform1 = go1.GetComponent<Transform>();
        transform1.Rotate(rotation);
    
        GameObject go2 = Instantiate(tallStairPrefab, location2, Quaternion.identity);
        Transform transform2 = go2.GetComponent<Transform>();
        transform2.Rotate(rotation);
    }
    void PlaceWalls()
    {
        foreach (Cell cell in grid)
        {
            if (cell.cellType == CellType.None) continue; 
            if (cell.cellType == CellType.Room)
                DrawBox(cell.location + Vector3.up * 0.5f, Quaternion.identity, new Vector3(1, 1, 1), Color.red);
            if (cell.cellType == CellType.Stairs)
                DrawBox(cell.location + Vector3.up * 0.5f, Quaternion.identity, new Vector3(1, 1, 1), Color.cyan);

            Vector3Int location = cell.bounds.position;
            bool hasPosXNeighbor = true;
            bool hasNegXNeighbor = true;
            bool hasPosZNeighbor = true;
            bool hasNegZNeighbor = true;
            bool hasPosYNeighbor = true;
            if (grid.HasItemAt(location + Vector3Int.right))
                hasPosXNeighbor = grid[location + Vector3Int.right].cellType == CellType.None;
            if (grid.HasItemAt(location + Vector3Int.left))
                hasNegXNeighbor = grid[location + Vector3Int.left].cellType == CellType.None;
            if (grid.HasItemAt(location + Vector3Int.forward))
                hasPosZNeighbor = grid[location + Vector3Int.forward].cellType == CellType.None;
            if (grid.HasItemAt(location + Vector3Int.back))
                hasNegZNeighbor = grid[location + Vector3Int.back].cellType == CellType.None;
            if (grid.HasItemAt(location + Vector3Int.up))
                hasPosYNeighbor = grid[location + Vector3Int.up].cellType == CellType.None;
            cell.PosX = hasPosXNeighbor;
            cell.NegX = hasNegXNeighbor;
            cell.PosZ = hasPosZNeighbor;
            cell.NegZ = hasNegZNeighbor;
            if (hasPosXNeighbor)
            {
                GameObject go = Instantiate(wallPrefab, location, Quaternion.identity);
                Transform transform = go.GetComponent<Transform>();
                transform.Rotate(new Vector3Int(0, -90, 0));
            }
            if (hasNegXNeighbor)
            {
                GameObject go = Instantiate(wallPrefab, location, Quaternion.identity);
                Transform transform = go.GetComponent<Transform>();
                transform.Rotate(new Vector3Int(0, 90, 0));
            }
            if (hasPosZNeighbor)
            {
                GameObject go = Instantiate(wallPrefab, location, Quaternion.identity);
                Transform transform = go.GetComponent<Transform>();
                transform.Rotate(new Vector3Int(0, 180, 0));
            }
            if (hasNegZNeighbor)
            {
                GameObject go = Instantiate(wallPrefab, location, Quaternion.identity);
                Transform transform = go.GetComponent<Transform>();
                transform.Rotate(new Vector3Int(0, 0, 0));
            }

            if (hasPosYNeighbor)
            {
                Instantiate(roofPrefab, location, Quaternion.identity);
            }
        }
    }
}
