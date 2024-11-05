using System.Collections.Generic;
using UnityEngine;

public class MazeAStarPathfinding : MonoBehaviour
{
    public Transform startPoint; // Starting point of the path
    public Transform endPoint;   // Endpoint of the path

    private List<Vector3> path = new List<Vector3>();

    void Start()
    {
        FindPath();
    }

    public void FindPath()
    {
        Node startNode = new Node(startPoint.position);
        Node targetNode = new Node(endPoint.position);
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || 
                    openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.Position == targetNode.Position)
            {
                RetracePath(startNode, currentNode);
                return;
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor) || !IsWalkable(neighbor.Position))
                {
                    continue;
                }

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
    }

    void RetracePath(Node startNode, Node endNode)
    {
        path.Clear();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        // Optionally visualize the path
        foreach (Vector3 point in path)
        {
            Debug.DrawLine(point, point + Vector3.up * 2, Color.red, 5f);
        }
    }

    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        // Define neighbor offsets
        Vector3[] offsets = {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 neighborPosition = node.Position + offset;
            neighbors.Add(new Node(neighborPosition));
        }

        return neighbors;
    }

    bool IsWalkable(Vector3 position)
    {
        // Check if the cell is walkable (not a wall)
        // You can implement logic to determine if a wall exists at this position.
        return true; // Placeholder logic, modify as needed.
    }

    int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.Position.x - b.Position.x);
        int dstY = Mathf.Abs(a.Position.z - b.Position.z);
        return dstX + dstY;
    }

    private class Node
    {
        public Vector3 Position;
        public int gCost;
        public int hCost;
        public Node parent;

        public Node(Vector3 position)
        {
            Position = position;
        }

        public int FCost => gCost + hCost;
    }
}
