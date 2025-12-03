using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private class Node
    {
        public int x, y;
        public Node parent;
        public int gCost;
        public int hCost;
        public int FCost => gCost + hCost;

        public Node(int x, int y) { this.x = x; this.y = y; }
    }

    public static List<Vector2Int> FindPath(GridSystem grid, Vector2Int startPos, Vector2Int targetPos)
    {
        if (!grid.IsPositionWalkable(startPos)) return null;

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        Node startNode = new Node(startPos.x, startPos.y);
        Node targetNode = new Node(targetPos.x, targetPos.y);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost ||
                    (openList[i].FCost == currentNode.FCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(new Vector2Int(currentNode.x, currentNode.y));

            if (currentNode.x == targetNode.x && currentNode.y == targetNode.y)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode, grid))
            {
                if (closedList.Contains(neighborPos)) continue;

                if (!grid.IsPositionWalkable(neighborPos.x, neighborPos.y)) continue;

                int newMovementCostToNeighbor = currentNode.gCost + 1;

                Node neighborNode = openList.Find(n => n.x == neighborPos.x && n.y == neighborPos.y);

                if (neighborNode == null || newMovementCostToNeighbor < neighborNode.gCost)
                {
                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighborPos.x, neighborPos.y);
                        openList.Add(neighborNode);
                    }

                    neighborNode.gCost = newMovementCostToNeighbor;
                    neighborNode.hCost = GetDistance(neighborNode, targetNode);
                    neighborNode.parent = currentNode;
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(new Vector2Int(currentNode.x, currentNode.y));
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private static int GetDistance(Node a, Node b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> GetNeighbors(Node node, GridSystem grid)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = node.x + dx[i];
            int ny = node.y + dy[i];

            if (grid.IsValidBlockCoord(nx, ny))
            {
                neighbors.Add(new Vector2Int(nx, ny));
            }
        }
        return neighbors;
    }
}