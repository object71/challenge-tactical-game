

using UnityEngine;

public class PathfindingNode
{
    public Tile tile;
    public PathfindingNode parent;
    public bool passed;
    public int distance;
}