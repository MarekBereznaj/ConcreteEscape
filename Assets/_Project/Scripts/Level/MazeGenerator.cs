using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Size")]
    [Min(2)] public int width = 15;
    [Min(2)] public int height = 15;

    [Header("Cell Settings")]
    [Min(0.5f)] public float cellSize = 3f;
    [Min(0.1f)] public float wallThickness = 0.3f;
    [Min(0.5f)] public float wallHeight = 2.5f;

    [Header("Prefabs")]
    public GameObject wallPrefab;   // Cube-based prefab recommended (with BoxCollider)
    public GameObject floorPrefab;  // Cube-based prefab recommended (with BoxCollider)

    [Header("Safety / Boundaries")]
    public bool createOuterBoundary = true;
    public bool createBigBaseFloor = true;
    public float baseFloorExtraMarginCells = 2f; // bigger safety floor around the maze

    [Header("Start / Exit")]
    public bool generateStartAndExit = true;
    public float markerHeight = 1.0f; // Y height for start/exit points
    public Vector3 exitTriggerSize = new Vector3(2f, 2f, 2f);

    // Internal: walls[x,y,dir] true = wall exists
    private bool[,,] walls;
    private bool[,] visited;

    private enum Dir { N = 0, E = 1, S = 2, W = 3 }

    private Transform root;
    private Transform floorRoot;
    private Transform wallsRoot;
    private Transform markersRoot;

    [ContextMenu("Generate Maze")]
    public void GenerateMaze()
    {
        if (wallPrefab == null || floorPrefab == null)
        {
            Debug.LogError("Assign Wall Prefab and Floor Prefab in the Inspector first.");
            return;
        }

        EnsureRoots();
        ClearGenerated();

        InitGrid();
        CarveMazeDFS();

        // Ensure maze is CLOSED on the perimeter (no openings outside)
        ForcePerimeterWallsClosed();

        BuildFloorTiles();
        BuildInteriorWalls();

        if (createOuterBoundary)
            BuildOuterBoundaryWalls(); // 4 long boundary walls = no falling out

        if (createBigBaseFloor)
            BuildBigBaseFloor(); // extra safety floor outside the maze

        if (generateStartAndExit)
            BuildStartAndExit();
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
            return;
        }
#endif
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    // -------------------- Generation --------------------

    private void EnsureRoots()
    {
        if (root != null) return;

        var existing = transform.Find("_Generated");
        if (existing != null) root = existing;
        else
        {
            var go = new GameObject("_Generated");
            go.transform.SetParent(transform, false);
            root = go.transform;
        }

        floorRoot = null;
        wallsRoot = null;
        markersRoot = null;
    }

    private void CreateChildRoots()
    {
        floorRoot = new GameObject("Floor").transform;
        floorRoot.SetParent(root, false);

        wallsRoot = new GameObject("Walls").transform;
        wallsRoot.SetParent(root, false);

        markersRoot = new GameObject("Markers").transform;
        markersRoot.SetParent(root, false);
    }

    private void InitGrid()
    {
        visited = new bool[width, height];
        walls = new bool[width, height, 4];

        // start with all walls present
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int d = 0; d < 4; d++)
            walls[x, y, d] = true;
    }

    private void CarveMazeDFS()
    {
        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(0, 0);
        visited[start.x, start.y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var (next, dirFromCurrent) = neighbors[Random.Range(0, neighbors.Count)];

            // remove wall between current and next
            walls[current.x, current.y, (int)dirFromCurrent] = false;
            walls[next.x, next.y, (int)Opposite(dirFromCurrent)] = false;

            visited[next.x, next.y] = true;
            stack.Push(next);
        }
    }

    private List<(Vector2Int cell, Dir dirFromCurrent)> GetUnvisitedNeighbors(Vector2Int c)
    {
        var list = new List<(Vector2Int, Dir)>(4);

        if (c.y + 1 < height && !visited[c.x, c.y + 1]) list.Add((new Vector2Int(c.x, c.y + 1), Dir.N));
        if (c.x + 1 < width && !visited[c.x + 1, c.y]) list.Add((new Vector2Int(c.x + 1, c.y), Dir.E));
        if (c.y - 1 >= 0 && !visited[c.x, c.y - 1]) list.Add((new Vector2Int(c.x, c.y - 1), Dir.S));
        if (c.x - 1 >= 0 && !visited[c.x - 1, c.y]) list.Add((new Vector2Int(c.x - 1, c.y), Dir.W));

        return list;
    }

    private Dir Opposite(Dir d) => (Dir)(((int)d + 2) % 4);

    private void ForcePerimeterWallsClosed()
    {
        // bottom row: South walls
        for (int x = 0; x < width; x++)
            walls[x, 0, (int)Dir.S] = true;

        // top row: North walls
        for (int x = 0; x < width; x++)
            walls[x, height - 1, (int)Dir.N] = true;

        // left col: West walls
        for (int y = 0; y < height; y++)
            walls[0, y, (int)Dir.W] = true;

        // right col: East walls
        for (int y = 0; y < height; y++)
            walls[width - 1, y, (int)Dir.E] = true;
    }

    // -------------------- Build Geometry --------------------

    private void BuildFloorTiles()
    {
        CreateChildRoots();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var pos = CellCenterWorld(x, y) + new Vector3(0f, -0.1f, 0f);
            var tile = Instantiate(floorPrefab, pos, Quaternion.identity, floorRoot);
            tile.name = $"Floor_{x}_{y}";
            tile.transform.localScale = new Vector3(cellSize, 0.2f, cellSize);
            tile.isStatic = true;
        }
    }

    private void BuildInteriorWalls()
    {
        // build walls without duplicates:
        // - create North and East for each cell
        // - create South for y==0
        // - create West for x==0
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (walls[x, y, (int)Dir.N]) CreateWall(x, y, Dir.N);
            if (walls[x, y, (int)Dir.E]) CreateWall(x, y, Dir.E);
            if (y == 0 && walls[x, y, (int)Dir.S]) CreateWall(x, y, Dir.S);
            if (x == 0 && walls[x, y, (int)Dir.W]) CreateWall(x, y, Dir.W);
        }

        wallsRoot.gameObject.isStatic = true;
    }

    private void CreateWall(int x, int y, Dir dir)
    {
        Vector3 pos = CellCenterWorld(x, y);
        float half = cellSize * 0.5f;

        // shift to edge
        switch (dir)
        {
            case Dir.N: pos += new Vector3(0f, wallHeight * 0.5f, +half); break;
            case Dir.S: pos += new Vector3(0f, wallHeight * 0.5f, -half); break;
            case Dir.E: pos += new Vector3(+half, wallHeight * 0.5f, 0f); break;
            case Dir.W: pos += new Vector3(-half, wallHeight * 0.5f, 0f); break;
        }

        bool horizontal = (dir == Dir.N || dir == Dir.S);
        Vector3 size = horizontal
            ? new Vector3(cellSize + wallThickness, wallHeight, wallThickness)
            : new Vector3(wallThickness, wallHeight, cellSize + wallThickness);

        var wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
        wall.name = $"Wall_{dir}_{x}_{y}";
        wall.transform.localScale = size;
        wall.isStatic = true;
    }

    private void BuildOuterBoundaryWalls()
    {
        // 4 long walls around the maze => absolutely no slipping out between tiles
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;

        // center of maze in world space
        Vector3 origin = transform.position;
        Vector3 center = origin + new Vector3((width - 1) * cellSize * 0.5f, wallHeight * 0.5f, (height - 1) * cellSize * 0.5f);

        float halfW = mazeW * 0.5f;
        float halfH = mazeH * 0.5f;

        // north wall (along X)
        CreateBoundaryWall(
            name: "Boundary_North",
            pos: center + new Vector3(0f, 0f, +halfH + cellSize * 0.5f),
            size: new Vector3(mazeW + wallThickness, wallHeight, wallThickness)
        );

        // south
        CreateBoundaryWall(
            name: "Boundary_South",
            pos: center + new Vector3(0f, 0f, -halfH - cellSize * 0.5f),
            size: new Vector3(mazeW + wallThickness, wallHeight, wallThickness)
        );

        // east (along Z)
        CreateBoundaryWall(
            name: "Boundary_East",
            pos: center + new Vector3(+halfW + cellSize * 0.5f, 0f, 0f),
            size: new Vector3(wallThickness, wallHeight, mazeH + wallThickness)
        );

        // west
        CreateBoundaryWall(
            name: "Boundary_West",
            pos: center + new Vector3(-halfW - cellSize * 0.5f, 0f, 0f),
            size: new Vector3(wallThickness, wallHeight, mazeH + wallThickness)
        );
    }

    private void CreateBoundaryWall(string name, Vector3 pos, Vector3 size)
    {
        var w = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
        w.name = name;
        w.transform.localScale = size;
        w.isStatic = true;
    }

    private void BuildBigBaseFloor()
    {
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;

        float extra = baseFloorExtraMarginCells * cellSize;
        Vector3 origin = transform.position;

        // big base floor centered under maze
        Vector3 center = origin + new Vector3((width - 1) * cellSize * 0.5f, -0.2f, (height - 1) * cellSize * 0.5f);

        var baseFloor = Instantiate(floorPrefab, center, Quaternion.identity, floorRoot);
        baseFloor.name = "BaseFloor_Safety";
        baseFloor.transform.localScale = new Vector3(mazeW + extra, 0.2f, mazeH + extra);
        baseFloor.isStatic = true;
    }

    // -------------------- Start / Exit --------------------

    private void BuildStartAndExit()
    {
        // Start at (0,0)
        Vector2Int start = new Vector2Int(0, 0);

        // Exit = farthest reachable cell from start (nice gameplay)
        Vector2Int exit = FindFarthestCellFrom(start);

        // Create points
        var startGo = new GameObject("StartPoint");
        startGo.transform.SetParent(markersRoot, false);
        startGo.transform.position = CellCenterWorld(start.x, start.y) + new Vector3(0f, markerHeight, 0f);

        var exitGo = new GameObject("ExitPoint");
        exitGo.transform.SetParent(markersRoot, false);
        exitGo.transform.position = CellCenterWorld(exit.x, exit.y) + new Vector3(0f, markerHeight, 0f);

        // Visible markers (simple primitives so you always see them)
        var startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        startMarker.name = "StartMarker";
        startMarker.transform.SetParent(startGo.transform, false);
        startMarker.transform.localPosition = new Vector3(0f, -markerHeight + 0.1f, 0f);
        startMarker.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        DestroyImmediate(startMarker.GetComponent<Collider>()); // marker collider not needed

        var exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exitMarker.name = "ExitMarker";
        exitMarker.transform.SetParent(exitGo.transform, false);
        exitMarker.transform.localPosition = new Vector3(0f, -markerHeight + 0.1f, 0f);
        exitMarker.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        DestroyImmediate(exitMarker.GetComponent<Collider>());

        // Exit trigger (for win)
        var trigger = new GameObject("ExitTrigger");
        trigger.transform.SetParent(exitGo.transform, false);
        trigger.transform.localPosition = Vector3.zero;

        var box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = exitTriggerSize;
    }

    private Vector2Int FindFarthestCellFrom(Vector2Int start)
    {
        // BFS over cells using carved passages (no wall between cells)
        int[,] dist = new int[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            dist[x, y] = -1;

        var q = new Queue<Vector2Int>();
        dist[start.x, start.y] = 0;
        q.Enqueue(start);

        Vector2Int farthest = start;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d = dist[c.x, c.y];

            if (d > dist[farthest.x, farthest.y])
                farthest = c;

            foreach (var n in GetPassageNeighbors(c))
            {
                if (dist[n.x, n.y] != -1) continue;
                dist[n.x, n.y] = d + 1;
                q.Enqueue(n);
            }
        }

        // fallback: opposite corner
        if (farthest == start)
            farthest = new Vector2Int(width - 1, height - 1);

        return farthest;
    }

    private IEnumerable<Vector2Int> GetPassageNeighbors(Vector2Int c)
    {
        // Passage exists if wall is false in that direction
        if (c.y + 1 < height && walls[c.x, c.y, (int)Dir.N] == false) yield return new Vector2Int(c.x, c.y + 1);
        if (c.x + 1 < width && walls[c.x, c.y, (int)Dir.E] == false) yield return new Vector2Int(c.x + 1, c.y);
        if (c.y - 1 >= 0 && walls[c.x, c.y, (int)Dir.S] == false) yield return new Vector2Int(c.x, c.y - 1);
        if (c.x - 1 >= 0 && walls[c.x, c.y, (int)Dir.W] == false) yield return new Vector2Int(c.x - 1, c.y);
    }

    private Vector3 CellCenterWorld(int x, int y)
    {
        return transform.position + new Vector3(x * cellSize, 0f, y * cellSize);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        var gen = (MazeGenerator)target;

        if (GUILayout.Button("Generate Maze"))
        {
            gen.GenerateMaze();
            EditorUtility.SetDirty(gen);
        }

        if (GUILayout.Button("Clear Generated"))
        {
            gen.ClearGenerated();
            EditorUtility.SetDirty(gen);
        }
    }
}
#endif
