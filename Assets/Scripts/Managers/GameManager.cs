using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Tile selectedTile;

    public Unit[] availableUnits;
    private Map map;
    public PathfindingManager pathfinding;
    public Player[] players;
    public Player currentPlayer;
    public bool noMovingAfterTheFirstOne = false;
    public bool oneActionOnly = false;

    private Text healthIndicator;
    private Text movementIndicator;
    public GameObject overlay;

    private static GameManager instance;

    public static GameManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<GameManager>();
        }

        return instance;
    }

    private static WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    private static WaitForSeconds waitSeconds = new WaitForSeconds(1f);

    void Awake()
    {
        map = FindObjectOfType<Map>();
        pathfinding = FindObjectOfType<PathfindingManager>();
        healthIndicator = GameObject.FindGameObjectWithTag("health-points").GetComponent<Text>();
        movementIndicator = GameObject.FindGameObjectWithTag("move-points").GetComponent<Text>();

        overlay.SetActive(false);

        if (!map)
        {
            Debug.LogWarning("Map not found");
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        Unit prefab = availableUnits[Random.Range(0, availableUnits.Length)];

        CreateUnit(prefab, players[0], 5, 3);
        // CreateUnit(prefab, players[0], 6, 3);
        // CreateUnit(prefab, players[0], 7, 3);

        CreateUnit(prefab, players[1], 5, 5);
        CreateUnit(prefab, players[1], 6, 5);
        CreateUnit(prefab, players[1], 7, 5);


        CalculatePossibleEnemyMoves();

    }

    private void CreateUnit(Unit prefab, Player player, int x, int y)
    {
        Unit unit = Instantiate(prefab, new Vector3(x, y), Quaternion.identity, map.transform);
        unit.SetControllingPlayer(player);
        map.AddUnit(x, y, unit);
    }

    private bool GameIsOver()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile tile = map.GetTile(x, y);
                if (tile.unit && tile.unit.controllingPlayer != currentPlayer)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Update is called once per frame
    protected void Update()
    {
        if (!currentPlayer.isAI && Input.GetMouseButtonUp(0))
        {
            Tile tile = InteractionHelpers.GetClickedElement<Tile>();
            if (tile)
            {
                OnTileLeftClicked(tile);
            }
        }
        else if (!currentPlayer.isAI && Input.GetMouseButtonUp(1))
        {
            Tile tile = InteractionHelpers.GetClickedElement<Tile>();
            if (tile)
            {
                OnTileRightClicked(tile);

                if (!UnitsHaveActions())
                {
                    SwitchPlayer();
                }

            }
        }
    }

    public void OnTileLeftClicked(Tile tile)
    {
        if (tile.unit != null)
        {
            SelectTile(tile);
        }
        else
        {
            DeselectTile();
        }
    }

    private void SelectTile(Tile tile)
    {
        if (selectedTile)
        {
            selectedTile.SetIsSelected(false);
            SetUIUnitInformation(null);
        }

        if (!tile.unit.isMoving)
        {
            selectedTile = tile;
            selectedTile.SetIsSelected(true);

            pathfinding.CalculatePathsAndSetStates(selectedTile);
            SetUIUnitInformation(tile.unit);
        }
    }

    private void SetUIUnitInformation(Unit unit)
    {
        if (unit == null)
        {
            healthIndicator.text = "--/--";
            movementIndicator.text = "--/--";
        }
        else
        {
            healthIndicator.text = string.Format("{0}/{1}", unit.remainingHealthPoints, unit.totalHealthPoints);
            movementIndicator.text = string.Format("{0}/{1}", unit.remainingMovementPoints, unit.totalMovementPoints);
        }
    }

    private void DeselectTile()
    {
        if (selectedTile)
        {
            selectedTile.SetIsSelected(false);
            SetUIUnitInformation(null);
        }

        selectedTile = null;

        pathfinding.ResetPaths();
    }

    private void HandleMove(Tile currentTile, Tile targetTile)
    {
        if (noMovingAfterTheFirstOne && currentTile.unit.hasMoved)
        {
            return;
        }

        Vector2Int mapPosition = targetTile.GetMapPosition();

        Tile reachableTile = pathfinding.GetMaxReachableTile(mapPosition.x, mapPosition.y, currentTile.unit.remainingMovementPoints);
        Vector2Int reachablePosition = reachableTile.GetMapPosition();
        int reachableDistance = pathfinding.GetDistanceToNode(reachablePosition.x, reachablePosition.y);

        StartCoroutine(ExecuteMoveAction(reachableTile, currentTile, reachableDistance));

        targetTile.SetIsSelected(false);
        DeselectTile();

        if (oneActionOnly)
        {
            SwitchPlayer();
        }
    }

    private void HandleAttack(Tile currentTile, Tile targetTile)
    {
        Vector2Int coordinate = targetTile.GetMapPosition();
        if (!currentTile.unit.hasAttacked)
        {
            int distanceToTarget = pathfinding.GetDistanceToNode(coordinate.x, coordinate.y);
            if (distanceToTarget <= currentTile.unit.attackRange)
            {
                StartCoroutine(ExecuteAttackAction(targetTile, currentTile));

                targetTile.SetIsSelected(false);
                DeselectTile();

                if (oneActionOnly)
                {
                    SwitchPlayer();
                }
            }
            else if (distanceToTarget <= currentTile.unit.attackRange + currentTile.unit.remainingMovementPoints)
            {
                if (noMovingAfterTheFirstOne && currentTile.unit.hasMoved)
                {
                    return;
                }

                int distanceToMove = distanceToTarget - currentTile.unit.attackRange;

                if (distanceToMove > currentTile.unit.remainingMovementPoints)
                {
                    return;
                }

                if (distanceToMove < 7 && currentTile.unit.remainingMovementPoints >= 7)
                {
                    distanceToMove = 7;
                }
                else if (distanceToMove < 5 && currentTile.unit.remainingMovementPoints >= 5)
                {
                    distanceToMove = 5;
                }

                Vector2Int mapPosition = targetTile.GetMapPosition();
                Tile reachableTile = pathfinding.GetMaxReachableTile(mapPosition.x, mapPosition.y, distanceToMove);

                if (reachableTile == currentTile)
                {
                    return;
                }

                Vector2Int reachablePosition = reachableTile.GetMapPosition();
                int reachableDistance = pathfinding.GetDistanceToNode(reachablePosition.x, reachablePosition.y);

                StartCoroutine(ExecuteMoveThenAttack(targetTile, currentTile, reachableTile, reachableDistance));

                currentTile.SetIsSelected(false);
                reachableTile.SetIsSelected(false);
                DeselectTile();

                if (oneActionOnly)
                {
                    SwitchPlayer();
                }
            }
            else
            {
                HandleMove(currentTile, targetTile);
            }


        }
    }

    public void OnTileRightClicked(Tile targetTile)
    {
        Tile currentTile = selectedTile;
        if (currentTile == null || currentTile.unit == null)
        {
            return;
        }

        if (currentTile.unit.controllingPlayer != currentPlayer)
        {
            return;
        }
        else
        {
            if (targetTile.unit == null)
            {
                HandleMove(currentTile, targetTile);
            }
            else if (targetTile.unit != null && targetTile.unit.controllingPlayer != currentPlayer)
            {
                HandleAttack(currentTile, targetTile);
            }
        }
    }

    public void CheckGameOverState()
    {
        if (GameIsOver())
        {
            overlay.SetActive(true);
            Text gameOverText = GameObject.Find("GameOverText").GetComponent<Text>();
            gameOverText.text = $"Player {currentPlayer.playerId} won!";
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ExecuteMoveThenAttack(Tile targetTile, Tile sourceTile, Tile moveToTile, int reachableDistance)
    {
        yield return ExecuteMoveAction(moveToTile, sourceTile, reachableDistance);
        yield return ExecuteAttackAction(targetTile, moveToTile);
    }

    private IEnumerator ExecuteAttackAction(Tile defendingTile, Tile attackingTile)
    {
        attackingTile.unit.hasAttacked = true;

        while (defendingTile.unit && defendingTile.unit.isBeingAttacked)
        {
            yield return endOfFrame;
        }

        if (!defendingTile.unit)
        {
            attackingTile.unit.hasAttacked = false;
        }
        else
        {
            defendingTile.unit.Hit(attackingTile.unit.attackDamage);
        }

    }

    private IEnumerator ExecuteMoveAction(Tile targetTile, Tile fromTile, int distance)
    {
        pathfinding.SetUnitPathDirections(fromTile.unit, targetTile);
        fromTile.unit.PlayMoveAnimation();

        fromTile.unit.remainingMovementPoints -= distance;
        targetTile.unit = fromTile.unit;
        fromTile.unit = null;

        while (targetTile.unit.isMoving)
        {
            yield return endOfFrame;
        }
    }

    public void OnMouseEnterTile(Tile tile)
    {
        if (selectedTile && selectedTile.unit && selectedTile.unit.controllingPlayer == currentPlayer)
        {
            pathfinding.ShowTrail(tile);
        }
    }

    public void SwitchPlayer()
    {
        pathfinding.ResetPaths();
        ResetMovementPoints();

        currentPlayer = players[0] == currentPlayer ? players[1] : players[0];

        if (currentPlayer.isAI)
        {
            StartCoroutine(PlayAI());
        }
        else
        {
            CalculatePossibleEnemyMoves();
        }

    }

    private IEnumerator PlayAI()
    {

        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile tile = map.GetTile(x, y);

                if (tile.unit)
                {
                    OnTileLeftClicked(tile);
                    yield return waitSeconds;

                    OnTileRightClicked(FindClosestEnemy());
                    yield return waitSeconds;
                }

            }
        }

        DeselectTile();
        yield return waitSeconds;

        if (!oneActionOnly)
        {
            SwitchPlayer();
        }
    }

    private Tile FindClosestEnemy()
    {
        Tile closest = null;
        int closestDistance = int.MaxValue;

        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile tile = map.GetTile(x, y);

                if (tile.unit && tile.unit.controllingPlayer != currentPlayer)
                {
                    int distance = pathfinding.GetDistanceToNode(x, y);

                    if (!closest || distance < closestDistance)
                    {
                        closest = tile;
                    }
                }
            }
        }

        return closest;
    }

    private void CalculatePossibleEnemyMoves()
    {
        pathfinding.ClearPossibleEnemyMoves();

        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile currentTile = map.GetTile(x, y);
                if (currentTile.unit && currentTile.unit.controllingPlayer != currentPlayer)
                {
                    pathfinding.CalculateEnemyPathsAndSetStates(currentTile);
                }
            }
        }

    }

    private void ResetMovementPoints()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile tile = map.GetTile(x, y);

                if (tile.unit != null)
                {
                    tile.unit.hasAttacked = false;
                    tile.unit.remainingMovementPoints = tile.unit.totalMovementPoints;
                }
            }
        }
    }

    private bool UnitsHaveActions()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Tile tile = map.GetTile(x, y);

                if (tile.unit != null && tile.unit.controllingPlayer == currentPlayer)
                {
                    // only one move and it hasn't moved (not considering a case with a static unit)
                    if (noMovingAfterTheFirstOne && !tile.unit.hasMoved)
                    {
                        return true;
                    }

                    // has not attacked attack (not checking if enemies within range)
                    if (!tile.unit.hasAttacked)
                    {
                        return true;
                    }

                    // isn't surrounded and has more movement points
                    if (tile.unit.remainingMovementPoints >= 5 && !IsSurrounded(x, y, tile.unit.remainingMovementPoints >= 7))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsSurrounded(int x, int y, bool includeDiagonals)
    {
        for (int m = -1; m <= 1; m++)
        {
            for (int n = -1; n <= 1; n++)
            {
                if (m == 0 & n == 0)
                {
                    continue;
                }

                if (!includeDiagonals && Mathf.Abs(m) == Mathf.Abs(n))
                {
                    continue;
                }

                if (!map.GetTile(x + m, y + n).unit)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
