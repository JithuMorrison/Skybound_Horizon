using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    public int width = 10;
    public int height = 10;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Scaling")]
    public Vector3 wallScale = new Vector3(1, 1, 1);
    public Vector3 floorScale = new Vector3(1, 1, 1);

    [Header("Maze Start Position")]
    public Vector3 mazeStartPosition = new Vector3(66, 61, 89); 

    private Cell[,] grid;
    private List<Cell> path; 

    void Start()
    {
        GenerateMaze();
    }

    void GenerateMaze()
    {
        InitializeGrid();
        CreateOuterWalls();
        CreateAllWalls();
        path = GeneratePathAStar(grid[0, 0], grid[width - 1, height - 1]);
        CarveMazePath();
        CreateEntranceAndExit();
    }

    void InitializeGrid()
    {
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell(x, y);

                Vector3 floorPosition = mazeStartPosition + new Vector3(x, 0, y);
                Instantiate(floorPrefab, floorPosition, Quaternion.identity, transform).transform.localScale = floorScale;
            }
        }
    }

    void CreateOuterWalls()
    {
        for (int x = -1; x <= width; x++)
        {
            CreateWall(mazeStartPosition + new Vector3(x, 0.5f, height), Quaternion.identity);
            CreateWall(mazeStartPosition + new Vector3(x, 0.5f, -1), Quaternion.identity);
        }

        for (int y = -1; y <= height; y++)
        {
            CreateWall(mazeStartPosition + new Vector3(-1, 0.5f, y), Quaternion.Euler(0, 90, 0));
            CreateWall(mazeStartPosition + new Vector3(width, 0.5f, y), Quaternion.Euler(0, 90, 0));
        }
    }

    void CreateAllWalls()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < width - 1) CreateWall(mazeStartPosition + new Vector3(x + 0.5f, 0.5f, y), Quaternion.Euler(0, 90, 0));
                if (y < height - 1) CreateWall(mazeStartPosition + new Vector3(x, 0.5f, y + 0.5f), Quaternion.identity);
            }
        }
    }

    void CreateWall(Vector3 position, Quaternion rotation)
    {
        Instantiate(wallPrefab, position, rotation, transform).transform.localScale = wallScale;
    }

    List<Cell> GeneratePathAStar(Cell start, Cell goal)
    {
        var openSet = new List<Cell> { start };
        var closedSet = new HashSet<Cell>();
        start.GCost = 0;
        start.HCost = GetDistance(start, goal);

        while (openSet.Count > 0)
        {
            var current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < current.FCost || openSet[i].FCost == current.FCost && openSet[i].HCost < current.HCost)
                {
                    current = openSet[i];
                }
            }

            if (current == goal)
            {
                return RetracePath(start, goal);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor)) continue;

                int newCostToNeighbor = current.GCost + GetDistance(current, neighbor);
                if (newCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newCostToNeighbor;
                    neighbor.HCost = GetDistance(neighbor, goal);
                    neighbor.Parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return new List<Cell>(); 
    }

    void CarveMazePath()
    {
        foreach (var cell in path)
        {
            if (cell.Parent != null)
            {
                RemoveWall(cell, cell.Parent);
            }
        }
    }

    List<Cell> RetracePath(Cell start, Cell end)
    {
        var path = new List<Cell>();
        Cell current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }

    List<Cell> GetNeighbors(Cell cell)
    {
        var neighbors = new List<Cell>();

        if (cell.X > 0) neighbors.Add(grid[cell.X - 1, cell.Y]);
        if (cell.X < width - 1) neighbors.Add(grid[cell.X + 1, cell.Y]);
        if (cell.Y > 0) neighbors.Add(grid[cell.X, cell.Y - 1]);
        if (cell.Y < height - 1) neighbors.Add(grid[cell.X, cell.Y + 1]);

        return neighbors;
    }

    int GetDistance(Cell a, Cell b)
    {
        int distX = Mathf.Abs(a.X - b.X);
        int distY = Mathf.Abs(a.Y - b.Y);
        return distX + distY;
    }

    void RemoveWall(Cell current, Cell next)
    {
        Vector3 wallPosition = (current.X == next.X)
            ? mazeStartPosition + new Vector3(current.X, 0.5f, (current.Y + next.Y) / 2f)
            : mazeStartPosition + new Vector3((current.X + next.X) / 2f, 0.5f, current.Y);

        Quaternion wallRotation = (current.X == next.X) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

        Collider[] colliders = Physics.OverlapBox(wallPosition, new Vector3(0.5f, 0.5f, 0.1f), wallRotation);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Wall"))
            {
                Destroy(collider.gameObject);
            }
        }
    }

    void CreateEntranceAndExit()
    {
        RemoveWall(grid[0, 0], new Cell(-1, 0)); 
        RemoveWall(grid[width - 1, height - 1], new Cell(width, height - 1)); 
    }

    private class Cell
    {
        public int X;
        public int Y;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        public Cell Parent;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
            GCost = int.MaxValue;
            HCost = 0;
            Parent = null;
        }
    }
}
