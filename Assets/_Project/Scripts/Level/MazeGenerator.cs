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
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Materials (optional)")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material startMaterial;
    public Material exitMaterial;

    [Header("Floor Y")]
    public float tileFloorY = -0.1f;
    public float baseFloorY = -0.2f;

    [Header("Safety Base Floor")]
    public bool createBigBaseFloor = true;
    public float baseFloorExtraMarginCells = 2f;

    [Header("Start / Exit")]
    public bool generateStartAndExit = true;
    public float markerHeight = 1.0f;
    public Vector3 exitTriggerSize = new Vector3(2f, 2f, 2f);
    public bool createExitDoor = true;
    public Vector3 exitDoorScale = new Vector3(1.2f, 2.5f, 0.2f);

    [Header("Coins")]
    public GameObject coinPrefab;
    [Min(0)] public int coinCount = 10;

    [Tooltip("Y offset above cell center for the coin root spawn position.")]
    public float coinY = 0.5f;

    [Tooltip("Rotate coin on X axis on its VISUAL (degrees). Use -90 for flat coin.")]
    public float coinRotateX = -90f;

    [Tooltip("Multiply coin VISUAL scale by this value on spawn.")]
    public float coinScaleMultiplier = 3f;

    [Header("Anti Z-Fighting (Walls)")]
    [Tooltip("Small offset for walls at edges to avoid z-fighting/overlap.")]
    public float wallEdgeEpsilon = 0.01f;

    // grid
    private bool[,,] walls;
    private bool[,] visited;

    private enum Dir { N = 0, E = 1, S = 2, W = 3 }

    // roots
    private Transform root;
    private Transform floorRoot;
    private Transform wallsRoot;
    private Transform markersRoot;
    private Transform coinsRoot;

    [ContextMenu("Generate Maze")]
    public void GenerateMaze()
    {
        if (wallPrefab == null || floorPrefab == null)
        {
            Debug.LogError("MazeGenerator: Assign Wall Prefab and Floor Prefab in Inspector.");
            return;
        }

        EnsureRoot();
        ClearGenerated();

        InitGrid();
        CarveMazeDFS();

        // close perimeter, player can't leave maze through gaps
        ForcePerimeterWallsClosed();

        CreateChildRoots();

        BuildFloorTiles();
        BuildInteriorWalls();

        if (createBigBaseFloor)
            BuildBigBaseFloor();

        if (generateStartAndExit)
            BuildStartAndExit();
        else
            ResetCoinsRun(0);
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        if (root == null) return;

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

    // -------------------- Roots --------------------

    private void EnsureRoot()
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
    }

    private void CreateChildRoots()
    {
        floorRoot = new GameObject("Floor").transform;
        floorRoot.SetParent(root, false);

        wallsRoot = new GameObject("Walls").transform;
        wallsRoot.SetParent(root, false);

        markersRoot = new GameObject("Markers").transform;
        markersRoot.SetParent(root, false);

        coinsRoot = new GameObject("Coins").transform;
        coinsRoot.SetParent(root, false);
    }

    // -------------------- Generation --------------------

    private void InitGrid()
    {
        visited = new bool[width, height];
        walls = new bool[width, height, 4];

        // all walls present
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
        for (int x = 0; x < width; x++)
            walls[x, 0, (int)Dir.S] = true;

        for (int x = 0; x < width; x++)
            walls[x, height - 1, (int)Dir.N] = true;

        for (int y = 0; y < height; y++)
            walls[0, y, (int)Dir.W] = true;

        for (int y = 0; y < height; y++)
            walls[width - 1, y, (int)Dir.E] = true;
    }

    // -------------------- Build Geometry --------------------

    private void BuildFloorTiles()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var pos = CellCenterWorld(x, y) + new Vector3(0f, tileFloorY, 0f);
            var tile = Instantiate(floorPrefab, pos, Quaternion.identity, floorRoot);
            tile.name = $"Floor_{x}_{y}";
            tile.transform.localScale = new Vector3(cellSize, 0.2f, cellSize);
            tile.isStatic = true;

            ApplyMaterial(tile, floorMaterial);
        }
    }

    private void BuildInteriorWalls()
    {
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
        float eps = Mathf.Max(0f, wallEdgeEpsilon);

        switch (dir)
        {
            case Dir.N: pos += new Vector3(0f, wallHeight * 0.5f, +half + eps); break;
            case Dir.S: pos += new Vector3(0f, wallHeight * 0.5f, -half - eps); break;
            case Dir.E: pos += new Vector3(+half + eps, wallHeight * 0.5f, 0f); break;
            case Dir.W: pos += new Vector3(-half - eps, wallHeight * 0.5f, 0f); break;
        }

        bool horizontal = (dir == Dir.N || dir == Dir.S);
        Vector3 size = horizontal
            ? new Vector3(cellSize + wallThickness, wallHeight, wallThickness)
            : new Vector3(wallThickness, wallHeight, cellSize + wallThickness);

        var wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
        wall.name = $"Wall_{dir}_{x}_{y}";
        wall.transform.localScale = size;
        wall.isStatic = true;

        ApplyMaterial(wall, wallMaterial);
    }

    private void BuildBigBaseFloor()
    {
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;

        float extra = baseFloorExtraMarginCells * cellSize;
        Vector3 origin = transform.position;

        Vector3 center = origin + new Vector3((width - 1) * cellSize * 0.5f, baseFloorY, (height - 1) * cellSize * 0.5f);

        var baseFloor = Instantiate(floorPrefab, center, Quaternion.identity, floorRoot);
        baseFloor.name = "BaseFloor_Safety";
        baseFloor.transform.localScale = new Vector3(mazeW + extra, 0.2f, mazeH + extra);
        baseFloor.isStatic = true;

        ApplyMaterial(baseFloor, floorMaterial);
    }

    // -------------------- Start / Exit / Coins --------------------

    private void BuildStartAndExit()
    {
        Vector2Int start = new Vector2Int(0, 0);
        Vector2Int exit = FindFarthestCellFrom(start);

        var startGo = new GameObject("StartPoint");
        startGo.transform.SetParent(markersRoot, false);
        startGo.transform.position = CellCenterWorld(start.x, start.y) + new Vector3(0f, markerHeight, 0f);

        var exitGo = new GameObject("ExitPoint");
        exitGo.transform.SetParent(markersRoot, false);
        exitGo.transform.position = CellCenterWorld(exit.x, exit.y) + new Vector3(0f, markerHeight, 0f);

        var startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        startMarker.name = "StartMarker";
        startMarker.transform.SetParent(startGo.transform, false);
        startMarker.transform.localPosition = new Vector3(0f, -markerHeight + 0.12f, 0f);
        startMarker.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        RemoveColliderIfAny(startMarker);
        ApplyMaterial(startMarker, startMaterial);

        var exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exitMarker.name = "ExitMarker";
        exitMarker.transform.SetParent(exitGo.transform, false);
        exitMarker.transform.localPosition = new Vector3(0f, -markerHeight + 0.12f, 0f);
        exitMarker.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        RemoveColliderIfAny(exitMarker);
        ApplyMaterial(exitMarker, exitMaterial);

        if (createExitDoor)
        {
            var exitDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitDoor.name = "ExitDoor";
            exitDoor.transform.SetParent(exitGo.transform, false);
            exitDoor.transform.localPosition = new Vector3(0f, 0.02f, cellSize * 0.40f);
            exitDoor.transform.localScale = exitDoorScale;
            ApplyMaterial(exitDoor, exitMaterial);
        }

        var trigger = new GameObject("ExitTrigger");
        trigger.transform.SetParent(exitGo.transform, false);
        trigger.transform.localPosition = Vector3.zero;

        var box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = exitTriggerSize;

        var trigScript = trigger.AddComponent<ExitTrigger>();
        trigScript.playerTag = "Player";
        trigScript.freezeTimeOnWin = false;

        SpawnCoins(start, exit);
    }

    private void SpawnCoins(Vector2Int startCell, Vector2Int exitCell)
    {
        if (coinPrefab == null || coinCount <= 0)
        {
            ResetCoinsRun(0);
            return;
        }

        int spawned = 0;
        int tries = 0;
        var used = new HashSet<int>();

        while (spawned < coinCount && tries < coinCount * 80)
        {
            tries++;

            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            if (x == startCell.x && y == startCell.y) continue;
            if (x == exitCell.x && y == exitCell.y) continue;

            int key = x * 10000 + y;
            if (!used.Add(key)) continue;

            // root spawn position
            Vector3 pos = CellCenterWorld(x, y) + new Vector3(0f, coinY, 0f);

            var c = Instantiate(coinPrefab, pos, Quaternion.identity, coinsRoot);
            c.name = $"Coin_{x}_{y}";

            // ✅ ALWAYS modify the VISUAL transform (renderer), not just root
            Transform visual = GetVisualTransform(c);

            // local transform relative to coin root
            visual.localRotation = Quaternion.Euler(coinRotateX, 0f, 0f);
            visual.localScale *= coinScaleMultiplier;

            // if your prefab already has an offset, we add on top
            visual.localPosition += Vector3.zero;

            spawned++;
        }

        ResetCoinsRun(spawned);
    }

    // Finds a transform that actually renders the model (works with nested FBX)
    private Transform GetVisualTransform(GameObject rootObj)
    {
        var rend = rootObj.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.transform;
        return rootObj.transform;
    }

    private void ResetCoinsRun(int required)
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.ResetRun(required);
    }

    // -------------------- Helpers --------------------

    private void ApplyMaterial(GameObject go, Material mat)
    {
        if (go == null || mat == null) return;

        // apply to ALL renderers inside (important for FBX children)
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = mat;
    }

    private void RemoveColliderIfAny(GameObject go)
    {
        var c = go.GetComponent<Collider>();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (c != null) DestroyImmediate(c);
            return;
        }
#endif
        if (c != null) Destroy(c);
    }

    private Vector2Int FindFarthestCellFrom(Vector2Int start)
    {
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

        if (farthest == start)
            farthest = new Vector2Int(width - 1, height - 1);

        return farthest;
    }

    private IEnumerable<Vector2Int> GetPassageNeighbors(Vector2Int c)
    {
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
