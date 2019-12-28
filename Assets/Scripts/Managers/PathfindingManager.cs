using System.Collections.Generic;
using UnityEngine;

public class PathfindingManager : MonoBehaviour {

    private PathfindingNode[, ] graph;
    private PathfindingNode[] path;
    private List<PathfindingNode> unexplored;
    private Map map;

    protected void Awake () {
        map = FindObjectOfType<Map> ();

        graph = new PathfindingNode[map.width, map.height];

        path = new PathfindingNode[map.width * map.height];
        unexplored = new List<PathfindingNode> (map.width * map.height);
    }

    protected void Start () {
        InitGraph ();
    }

    public void InitiateNew () {
        graph = new PathfindingNode[map.width, map.height];

        path = new PathfindingNode[map.width * map.height];
        unexplored = new List<PathfindingNode> (map.width * map.height);

        InitGraph ();
    }

    public void ResetPaths () {
        ResetGraph ();
    }

    private void ResetGraph () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                PathfindingNode node = graph[x, y];
                node.distance = int.MaxValue;
                node.parent = null;
                node.passed = false;

                node.tile.SetIsInMoveRange (false);
                node.tile.SetIsUnitWithinRange (false);
                node.tile.SetIsCurrentPath (false);
                node.tile.SetIsWithinAttackRange (false);
            }
        }
    }

    private void InitGraph () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                graph[x, y] = new PathfindingNode () {
                    distance = int.MaxValue,
                    tile = map.GetTile (x, y),
                    parent = null,
                    passed = false
                };
            }
        }
    }

    public int GetDistanceToNode (int x, int y) {
        return graph[x, y].distance;
    }

    public Tile GetMaxReachableTile (int x, int y, int maxDistance) {
        PathfindingNode node = graph[x, y];
        if (node.distance > maxDistance) {
            while (node.parent != null) {
                if (node.distance <= maxDistance) {
                    break;
                }

                node.tile.SetIsCurrentPath (true);
                node = node.parent;
            }

        }

        return node.tile;
    }

    public void CalculatePathsAndSetStates (Tile tile) {
        DijkstraPathfinding (tile);
        SetTilesInMoveRange (tile);
        SetTilesInAttackRange (tile);
    }

    public void CalculateEnemyPathsAndSetStates (Tile tile) {
        DijkstraPathfinding (tile);
        SetTilesInEnemyRange (tile);
    }

    public void ClearPossibleEnemyMoves () {
        ResetTilesInEnemyRange ();
    }

    private void DijkstraPathfinding (Tile source) {
        unexplored.Clear ();
        ResetGraph ();

        Vector2Int sourceMapPosition = source.GetMapPosition ();

        graph[sourceMapPosition.x, sourceMapPosition.y].distance = 0;
        unexplored.Add (graph[sourceMapPosition.x, sourceMapPosition.y]);

        while (unexplored.Count > 0) {
            if (unexplored.Count > map.width * map.height) {
                break;
            }

            unexplored.Sort ((x, y) => {
                return x.distance - y.distance;
            });

            PathfindingNode lowest = unexplored[0];
            lowest.passed = true;

            unexplored.RemoveAt (0);

            Vector2Int mapPosition = lowest.tile.GetMapPosition ();

            // Note: not checking map bounds as they will be walled in anyway
            for (int xOffset = -1; xOffset <= 1; xOffset++) {
                for (int yOffset = -1; yOffset <= 1; yOffset++) {

                    int cost = 5;

                    if (xOffset == 0 && yOffset == 0) {
                        // current
                        continue;
                    }

                    if (Mathf.Abs (xOffset) == Mathf.Abs (yOffset)) {
                        // diagonal
                        cost = 7;
                    }

                    int nextNodeX = mapPosition.x + xOffset;
                    int nextNodeY = mapPosition.y + yOffset;

                    if (map.IsWithinBounds (nextNodeX, nextNodeY)) {
                        PathfindingNode nextNode = graph[nextNodeX, nextNodeY];

                        if (unexplored.Contains (nextNode)) {
                            if (nextNode.distance > lowest.distance + cost) {
                                nextNode.distance = lowest.distance + cost;
                            }
                        } else if (!nextNode.passed && nextNode.tile.IsWalkable () && nextNode.tile.IsFree ()) {
                            nextNode.distance = lowest.distance + cost;
                            nextNode.parent = lowest;
                            unexplored.Add (nextNode);
                        } else if (!nextNode.passed && nextNode.tile.IsOccupied ()) {
                            nextNode.distance = lowest.distance + cost;
                            nextNode.parent = lowest;
                            nextNode.passed = true;
                        }
                    }

                }
            }
        }
    }

    private void SetTilesInMoveRange (Tile tile) {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                PathfindingNode node = graph[x, y];
                int currentDistance = node.distance;
                if (node.tile.unit != null) {
                    if (currentDistance <= tile.unit.remainingMovementPoints + tile.unit.attackRange) {
                        node.tile.SetIsUnitWithinRange (true);
                    }
                } else if (currentDistance <= tile.unit.remainingMovementPoints) {
                    map.GetTile (x, y).SetIsInMoveRange (true);
                } else {
                    map.GetTile (x, y).SetIsInMoveRange (false);
                }
            }
        }
    }

    private void SetTilesInAttackRange (Tile tile) {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                PathfindingNode node = graph[x, y];
                int currentDistance = node.distance;

                if (currentDistance <= tile.unit.attackRange) {
                    node.tile.SetIsWithinAttackRange (true);
                }
            }
        }
    }

    private void SetTilesInEnemyRange (Tile tile) {
        int maxRange = tile.unit.totalMovementPoints + tile.unit.attackRange;

        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                PathfindingNode node = graph[x, y];
                int currentDistance = node.distance;

                if (currentDistance <= maxRange) {
                    node.tile.SetIsWithinEnemyMoveRange (true);
                }
            }
        }
    }

    private void ResetTilesInEnemyRange () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                PathfindingNode node = graph[x, y];
                int currentDistance = node.distance;

                node.tile.SetIsWithinEnemyMoveRange (false);
            }
        }
    }

    public void SetUnitPathDirections (Unit unit, Tile targetTile) {
        Vector2Int position = targetTile.GetMapPosition ();

        PathfindingNode node = graph[position.x, position.y];
        while (node.parent != null) {
            unit.movementPath.Add (node.tile.transform.position);
            node = node.parent;
        }

        unit.movementPath.Reverse ();
    }

    public void ShowTrail (Tile tile) {
        ClearTrail ();
        MakeNewTrail (tile);
    }

    private void MakeNewTrail (Tile tile) {
        Vector2Int mapPosition = tile.GetMapPosition ();
        PathfindingNode node = graph[mapPosition.x, mapPosition.y];
        while (node.parent != null) {
            node.tile.SetIsCurrentPath (true);
            node = node.parent;
        }
    }

    private void ClearTrail () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                map.GetTile (x, y).SetIsCurrentPath (false);
            }
        }
    }
}