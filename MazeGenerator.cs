using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public GameObject wallPrefab;
    public GameObject pathPrefab;

    private Cell[,] grid;

    void Start()
    {
        GenerateMaze();
    }

    void GenerateMaze()
    {
        grid = new Cell[width, height];

        // Initialize the grid with walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell(x, y);
                Instantiate(wallPrefab, new Vector3(x, 0, y), Quaternion.identity);
            }
        }

        // Start from a random cell
        List<Cell> walls = new List<Cell>();
        int startX = Random.Range(0, width);
        int startY = Random.Range(0, height);
        grid[startX, startY].IsVisited = true;

        AddWalls(startX, startY, walls);

        while (walls.Count > 0)
        {
            int randomIndex = Random.Range(0, walls.Count);
            Cell currentWall = walls[randomIndex];

            List<Cell> neighbors = GetUnvisitedNeighbors(currentWall);
            if (neighbors.Count > 0)
            {
                // Randomly select one neighbor
                Cell neighbor = neighbors[Random.Range(0, neighbors.Count)];

                // Remove wall between currentWall and neighbor
                RemoveWall(currentWall, neighbor);

                // Mark neighbor as visited
                neighbor.IsVisited = true;

                // Add new walls
                AddWalls(neighbor.X, neighbor.Y, walls);
            }

            walls.RemoveAt(randomIndex);
        }

        // Generate a path for visualization
        GeneratePath();
    }

    void AddWalls(int x, int y, List<Cell> walls)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int direction in directions)
        {
            int newX = x + direction.x;
            int newY = y + direction.y;

            if (IsInBounds(newX, newY) && !grid[newX, newY].IsVisited)
            {
                walls.Add(grid[x + direction.x, y + direction.y]);
            }
        }
    }

    List<Cell> GetUnvisitedNeighbors(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int direction in directions)
        {
            int newX = cell.X + direction.x;
            int newY = cell.Y + direction.y;

            if (IsInBounds(newX, newY) && !grid[newX, newY].IsVisited)
            {
                neighbors.Add(grid[newX, newY]);
            }
        }

        return neighbors;
    }

    void RemoveWall(Cell current, Cell neighbor)
    {
        Vector2Int direction = new Vector2Int(neighbor.X - current.X, neighbor.Y - current.Y);
        if (direction == Vector2Int.up) DestroyWall(current.X, current.Y, current.X, current.Y + 1);
        else if (direction == Vector2Int.down) DestroyWall(current.X, current.Y, current.X, current.Y - 1);
        else if (direction == Vector2Int.left) DestroyWall(current.X, current.Y, current.X - 1, current.Y);
        else if (direction == Vector2Int.right) DestroyWall(current.X, current.Y, current.X + 1, current.Y);
    }

    void DestroyWall(int x1, int y1, int x2, int y2)
    {
        // Destroy wall between two cells
        // Calculate the position of the wall and destroy it
        Vector3 wallPosition = new Vector3((x1 + x2) / 2f, 0, (y1 + y2) / 2f);
        Collider[] hitColliders = Physics.OverlapBox(wallPosition, new Vector3(0.5f, 1f, 0.5f));
        foreach (var hitCollider in hitColliders)
        {
            Destroy(hitCollider.gameObject);
        }
    }

    void GeneratePath()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].IsVisited)
                {
                    Instantiate(pathPrefab, new Vector3(x, 0, y), Quaternion.identity);
                }
            }
        }
    }

    bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private class Cell
    {
        public int X;
        public int Y;
        public bool IsVisited;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
            IsVisited = false;
        }
    }
}
