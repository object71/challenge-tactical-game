using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public Tile selectedTile;

    public Unit[] availableUnits;
    public Map map;
    private Player[] players;

    private PathfindingNode[,] graph;
    private PathfindingNode[] path;
    private List<PathfindingNode> unexplored;
    private Player currentPlayer;

    private static GameManager instance;
    public static GameManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<GameManager>();
        }

        return instance;
    }

    void Awake()
    {
        map = FindObjectOfType<Map>();
        players = FindObjectsOfType<Player>();

        currentPlayer = players[0];

        graph = new PathfindingNode[map.width, map.height];

        path = new PathfindingNode[map.width * map.height];
        unexplored = new List<PathfindingNode>(map.width * map.height);

        if (!map)
        {
            Debug.LogWarning("Map not found");
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        Unit prefab = availableUnits[Random.Range(0, availableUnits.Length)];

        Unit unit = Instantiate(prefab, new Vector3(3f, 3f), Quaternion.identity, map.transform);
        unit.SetControllingPlayer(players[0]);
        map.AddUnit(3, 3, unit);

        Unit enemyUnit = Instantiate(prefab, new Vector3(5f, 5f), Quaternion.identity, map.transform);
        enemyUnit.SetControllingPlayer(players[1]);
        map.AddUnit(5, 5, enemyUnit);

        InitGraph();
    }

    // Update is called once per frame
    protected void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Tile tile = GetClickedElement<Tile>();
            if (tile)
            {
                OnTileLeftClicked(tile);
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Tile tile = GetClickedElement<Tile>();
            if (tile)
            {
                OnTileRightClicked(tile);
            }
        }
    }

    public T GetClickedElement<T>()
        where T : class
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null)
        {
            return hit.collider.gameObject.GetComponent<T>();
        }

        return null;
    }

    public void OnTileLeftClicked(Tile tile)
    {
        if (tile.unit != null)
        {
            if (selectedTile)
            {
                selectedTile.SetIsSelected(false);
            }

            if (!tile.unit.isMoving)
            {
                selectedTile = tile;
                selectedTile.SetIsSelected(true);

                DijkstraPathfinding(selectedTile);
                SetTilesInMoveRange(selectedTile);
            }

        }
        else
        {
            if (selectedTile)
            {
                selectedTile.SetIsSelected(false);
            }

            selectedTile = null;

            ResetGraph();
        }
    }

    private void SetTilesInMoveRange(Tile tile)
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                PathfindingNode node = graph[x, y];
                int currentDistance = node.distance;
                if (node.tile.unit != null)
                {
                    if (node.tile.unit.controllingPlayer != currentPlayer && currentDistance <= tile.unit.remainingMovementPoints + tile.unit.attackRange)
                    {
                        node.tile.SetIsEnemyWithinRange(true);
                    }
                }
                else if (currentDistance <= tile.unit.remainingMovementPoints)
                {
                    map.GetTile(x, y).SetIsInMoveRange(true);
                }
                else
                {
                    map.GetTile(x, y).SetIsInMoveRange(false);
                }
            }
        }
    }

    public void OnTileRightClicked(Tile tile)
    {
        if (tile.unit == null && selectedTile != null && selectedTile.unit.controllingPlayer == currentPlayer)
        {
            Vector2Int mapPosition = tile.GetMapPosition();
            int distance = graph[mapPosition.x, mapPosition.y].distance;
            Tile targetTile = tile;

            if (distance > selectedTile.unit.remainingMovementPoints)
            {
                PathfindingNode node = graph[tile.GetMapPosition().x, tile.GetMapPosition().y];
                while (node.parent != null)
                {
                    if (node.distance <= selectedTile.unit.remainingMovementPoints)
                    {
                        break;
                    }

                    node.tile.SetIsCurrentPath(true);
                    node = node.parent;
                }

                targetTile = node.tile;
            }

            MoveToTile(targetTile, mapPosition, distance);
        }
        else if (tile.unit != null && tile.unit.controllingPlayer != currentPlayer && selectedTile != null)
        {
            Vector2Int coordinate = tile.GetMapPosition();
            if (!selectedTile.unit.hasAttacked && graph[coordinate.x, coordinate.y].distance <= selectedTile.unit.attackRange)
            {
                tile.unit.remainingHealthPoints -= selectedTile.unit.attackDamage;
                selectedTile.unit.hasAttacked = true;

                tile.unit.PlayBlinkAnimation();
                selectedTile.unit.PlayBlinkAnimation();

                if (tile.unit.remainingHealthPoints <= 0)
                {
                    Destroy(tile.unit.gameObject);
                }
            }
        }
    }

    private void MoveToTile(Tile targetTile, Vector2Int mapPosition, int distance)
    {
        Unit unit = selectedTile.unit;
        unit.MoveToTile(targetTile, graph);

        selectedTile.unit.remainingMovementPoints -= distance;

        targetTile.unit = unit;
        selectedTile.unit = null;

        selectedTile.SetIsSelected(false);
        selectedTile = null;

        ResetGraph();
    }

    public void DijkstraPathfinding(Tile source)
    {
        unexplored.Clear();
        ResetGraph();

        Vector2Int sourceMapPosition = source.GetMapPosition();

        graph[sourceMapPosition.x, sourceMapPosition.y].distance = 0;
        unexplored.Add(graph[sourceMapPosition.x, sourceMapPosition.y]);

        while (unexplored.Count > 0)
        {
            if (unexplored.Count > map.width * map.height)
            {
                break;
            }

            unexplored.Sort((x, y) =>
            {
                return x.distance - y.distance;
            });

            PathfindingNode lowest = unexplored.First();
            lowest.passed = true;

            unexplored.RemoveAt(0);

            Vector2Int mapPosition = lowest.tile.GetMapPosition();

            // Note: not checking map bounds as they will be walled in anyway
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {

                    int cost = 5;

                    if (xOffset == 0 && yOffset == 0)
                    {
                        // current
                        continue;
                    }

                    if (Mathf.Abs(xOffset) == Mathf.Abs(yOffset))
                    {
                        // diagonal
                        cost = 7;
                    }

                    int nextNodeX = mapPosition.x + xOffset;
                    int nextNodeY = mapPosition.y + yOffset;

                    if (map.IsWithinBounds(nextNodeX, nextNodeY))
                    {
                        PathfindingNode nextNode = graph[nextNodeX, nextNodeY];

                        if (unexplored.Contains(nextNode))
                        {
                            if (nextNode.distance > lowest.distance + cost)
                            {
                                nextNode.distance = lowest.distance + cost;
                            }
                        }
                        else if (!nextNode.passed && nextNode.tile.IsWalkable() && nextNode.tile.IsFree())
                        {
                            nextNode.distance = lowest.distance + cost;
                            nextNode.parent = lowest;
                            unexplored.Add(nextNode);
                        }
                        else if (!nextNode.passed && nextNode.tile.IsOccupied())
                        {
                            nextNode.distance = lowest.distance + cost;
                            nextNode.parent = lowest;
                            nextNode.passed = true;
                        }
                    }

                }
            }
        }
    }

    public void OnMouseEnterTile(Tile tile)
    {
        if (selectedTile && selectedTile.unit.controllingPlayer == currentPlayer)
        {
            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    map.GetTile(x, y).SetIsCurrentPath(false);
                }
            }

            PathfindingNode node = graph[tile.GetMapPosition().x, tile.GetMapPosition().y];
            while (node.parent != null)
            {
                node.tile.SetIsCurrentPath(true);
                node = node.parent;
            }
        }
    }

    private void ResetGraph()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                PathfindingNode node = graph[x, y];
                node.distance = int.MaxValue;
                node.parent = null;
                node.passed = false;

                node.tile.SetIsInMoveRange(false);
                node.tile.SetIsEnemyWithinRange(false);
                node.tile.SetIsCurrentPath(false);
            }
        }
    }

    private void InitGraph()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                graph[x, y] = new PathfindingNode()
                {
                    distance = int.MaxValue,
                    tile = map.GetTile(x, y),
                    parent = null,
                    passed = false
                };
            }
        }
    }
}
