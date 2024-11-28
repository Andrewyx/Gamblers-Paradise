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
    [SerializeField] private GameObject stairPrefab;
    [SerializeField] private GameObject tallStairPrefab;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject wallPrefab;
    
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
                    grid[pos] = newRoom;
                }
                rooms.Add(newRoom);
                startPlaced = true;
            }
            else if (add) {
                rooms.Add(newRoom);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = newRoom;
                }
            }
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
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

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
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
                } else if (grid.HasItemAt(a.Position)){
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
                        grid[current].cellType = CellType.Hallway;
                    }

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                        int xDir = Mathf.Clamp(delta.x, -1, 1);
                        int zDir = Mathf.Clamp(delta.z, -1, 1);
                        Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                        Vector3 rot = new Vector3();
                        Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);
                        
                        if (delta.y > 0)
                        {
                            grid[prev + horizontalOffset] =  new Stair(prev + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + horizontalOffset * 2] = new Stair(prev + horizontalOffset * 2, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset] = new Stair(prev + verticalOffset + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset * 2] = new Stair(prev + verticalOffset + horizontalOffset * 2, new Vector3Int(1, 1, 1));

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
                            grid[prev + horizontalOffset] =  new Stair(prev + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + horizontalOffset * 2] = new Stair(prev + horizontalOffset * 2, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset] = new Stair(prev + verticalOffset + horizontalOffset, new Vector3Int(1, 1, 1));
                            grid[prev + verticalOffset + horizontalOffset * 2] = new Stair(prev + verticalOffset + horizontalOffset * 2, new Vector3Int(1, 1, 1));
                            
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

                        Debug.DrawLine(prev, current, Color.blue, 1000, false);
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
        Vector3 location = (Vector3)locationInt + new Vector3((size.x - 1) / 2f, Mathf.Floor(size.y/2f), (size.z - 1) / 2f);
        GameObject go = Instantiate(spawnPrefab, location, Quaternion.identity);
        go.GetComponent<Transform>().localScale = size;
        GameObject p = Instantiate(playerPrefab, location + Vector3.up/4, Quaternion.identity);
        p.GetComponent<Transform>().localScale = p.GetComponent<Transform>().localScale/5;
    }
    void PlaceRoom(Vector3Int locationInt, Vector3Int size) {
        Vector3 location = (Vector3)locationInt + new Vector3((size.x - 1) / 2f, Mathf.Floor(size.y/2f), (size.z - 1) / 2f);
        GameObject go = Instantiate(cubePrefab, (Vector3)location, Quaternion.identity);
        go.GetComponent<Transform>().localScale = size;
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
        
    }
}
