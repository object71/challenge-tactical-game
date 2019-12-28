using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    public Tile selectedTile;
    private Map map;
    public PathfindingManager pathfinding;
    public Player[] players;
    public Player currentPlayer;
    public bool noMovingAfterTheFirstOne = false;
    public bool oneActionOnly = false;
    public GameObject gameOverOverlay;
    public GameObject levelsOverlay;
    public Dropdown levelChooser;

    private Text healthIndicator;
    private Text movementIndicator;

    private static GameManager instance;
    public static GameManager GetInstance () {
        if (instance == null) {
            instance = FindObjectOfType<GameManager> ();
        }

        return instance;
    }

    private static readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame ();
    private static readonly WaitForSeconds waitSeconds = new WaitForSeconds (1f);

    void Awake () {
        map = FindObjectOfType<Map> ();
        pathfinding = FindObjectOfType<PathfindingManager> ();
        healthIndicator = GameObject.FindGameObjectWithTag ("health-points").GetComponent<Text> ();
        movementIndicator = GameObject.FindGameObjectWithTag ("move-points").GetComponent<Text> ();

        gameOverOverlay.SetActive (false);

        if (!map) {
            Debug.LogWarning ("Map not found");
        }

        if (!levelChooser) {
            Debug.LogWarning ("Level dropdown not found");
        }

        if (!pathfinding) {
            Debug.LogWarning ("Pathfinding manager not found");
        }

        if (players.Length < 2) {
            Debug.LogWarning ("Not enough players to play the game");
        }

        if (gameOverOverlay) {
            Debug.LogWarning ("No game over screen");
        }

        if (levelsOverlay) {
            Debug.LogWarning ("No level choosing screen");
        }

        levelChooser.options.Clear ();
        levelChooser.options.AddRange (System.IO.Directory.GetFiles ("./levels").Select (x => new Dropdown.OptionData (x)).ToArray ());
    }

    // Start is called before the first frame update
    protected void Start () { }

    private bool GameIsOver () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile tile = map.GetTile (x, y);
                if (tile.unit && tile.unit.controllingPlayer != currentPlayer) {
                    return false;
                }
            }
        }

        return true;
    }

    // Update is called once per frame
    protected void Update () {
        if (!map || !pathfinding) {
            return;
        }

        if (!currentPlayer.isAI && Input.GetMouseButtonUp (0)) {
            Tile tile = InteractionHelpers.GetClickedElement<Tile> ();
            if (tile) {
                OnTileLeftClicked (tile);
            }
        } else if (!currentPlayer.isAI && Input.GetMouseButtonUp (1)) {
            Tile tile = InteractionHelpers.GetClickedElement<Tile> ();
            if (tile) {
                OnTileRightClicked (tile);

                if (!UnitsHaveActions ()) {
                    SwitchPlayer ();
                }

            }
        }
    }

    public void OnTileLeftClicked (Tile tile) {
        if (tile.unit != null) {
            SelectTile (tile);
        } else {
            DeselectTile ();
        }
    }

    public void LoadLevel () {
        string chosenLevel = levelChooser.options[levelChooser.value].text;
        string level = System.IO.File.ReadAllText (chosenLevel);
        map.LoadMap (level);
        pathfinding.InitiateNew ();

        levelsOverlay.SetActive (false);

        CalculatePossibleEnemyMoves ();
    }

    private void SelectTile (Tile tile) {
        if (selectedTile) {
            selectedTile.SetIsSelected (false);
            SetUIUnitInformation (null);
        }

        if (!tile.unit.isMoving) {
            selectedTile = tile;
            selectedTile.SetIsSelected (true);

            pathfinding.CalculatePathsAndSetStates (selectedTile);
            SetUIUnitInformation (tile.unit);
        }
    }

    private void SetUIUnitInformation (Unit unit) {
        if (unit == null) {
            healthIndicator.text = "--/--";
            movementIndicator.text = "--/--";
        } else {
            healthIndicator.text = string.Format ("{0}/{1}", unit.remainingHealthPoints, unit.totalHealthPoints);
            movementIndicator.text = string.Format ("{0}/{1}", unit.remainingMovementPoints, unit.totalMovementPoints);
        }
    }

    private void DeselectTile () {
        if (selectedTile) {
            selectedTile.SetIsSelected (false);
            SetUIUnitInformation (null);
        }

        selectedTile = null;

        pathfinding.ResetPaths ();
    }

    private void HandleMove (Tile currentTile, Tile targetTile) {
        if (currentTile == targetTile) {
            DeselectTile ();
        }

        if (noMovingAfterTheFirstOne && currentTile.unit.hasMoved) {
            return;
        }

        Vector2Int mapPosition = targetTile.GetMapPosition ();

        Tile reachableTile = pathfinding.GetMaxReachableTile (mapPosition.x, mapPosition.y, currentTile.unit.remainingMovementPoints);
        if (reachableTile == currentTile) {
            return;
        }

        Vector2Int reachablePosition = reachableTile.GetMapPosition ();
        int reachableDistance = pathfinding.GetDistanceToNode (reachablePosition.x, reachablePosition.y);

        StartCoroutine (ExecuteMoveAction (reachableTile, currentTile, reachableDistance));

        targetTile.SetIsSelected (false);
        DeselectTile ();

        if (oneActionOnly) {
            SwitchPlayer ();
        }
    }

    private void HandleAttack (Tile currentTile, Tile targetTile) {
        Vector2Int coordinate = targetTile.GetMapPosition ();
        if (!currentTile.unit.hasAttacked) {
            int distanceToTarget = pathfinding.GetDistanceToNode (coordinate.x, coordinate.y);
            if (distanceToTarget <= currentTile.unit.attackRange) {
                StartCoroutine (ExecuteAttackAction (targetTile, currentTile));

                targetTile.SetIsSelected (false);
                DeselectTile ();

                if (oneActionOnly) {
                    SwitchPlayer ();
                }
            } else if (distanceToTarget <= currentTile.unit.attackRange + currentTile.unit.remainingMovementPoints) {
                if (noMovingAfterTheFirstOne && currentTile.unit.hasMoved) {
                    return;
                }

                int distanceToMove = Mathf.Min (distanceToTarget - 1, currentTile.unit.remainingMovementPoints);

                Vector2Int mapPosition = targetTile.GetMapPosition ();
                Tile reachableTile = pathfinding.GetMaxReachableTile (mapPosition.x, mapPosition.y, distanceToMove);

                // if the unit can't pay the cost in that direction
                if (reachableTile == currentTile) {
                    return;
                }

                Vector2Int reachablePosition = reachableTile.GetMapPosition ();
                int reachableDistance = pathfinding.GetDistanceToNode (reachablePosition.x, reachablePosition.y);

                // if the distance we travel is still not enough (could happen because of different costs on diagonals)
                if (reachableDistance < distanceToTarget - currentTile.unit.attackRange) {
                    return;
                }

                StartCoroutine (ExecuteMoveThenAttack (targetTile, currentTile, reachableTile, reachableDistance));

                currentTile.SetIsSelected (false);
                reachableTile.SetIsSelected (false);
                DeselectTile ();

                if (oneActionOnly) {
                    SwitchPlayer ();
                }
            } else {
                HandleMove (currentTile, targetTile);
            }

        }
    }

    public void OnTileRightClicked (Tile targetTile) {
        Tile currentTile = selectedTile;
        if (currentTile == null || currentTile.unit == null) {
            return;
        }

        if (currentTile.unit.controllingPlayer != currentPlayer) {
            return;
        } else {
            if (targetTile.unit == null) {
                HandleMove (currentTile, targetTile);
            } else if (targetTile.unit != null && targetTile.unit.controllingPlayer != currentPlayer) {
                HandleAttack (currentTile, targetTile);
            }
        }
    }

    public void CheckGameOverState () {
        if (GameIsOver ()) {
            gameOverOverlay.SetActive (true);
            Text gameOverText = GameObject.Find ("GameOverText").GetComponent<Text> ();
            gameOverText.text = $"Player {currentPlayer.playerId} won!";
        }
    }

    public void RestartGame () {
        SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex);
    }

    private IEnumerator ExecuteMoveThenAttack (Tile targetTile, Tile sourceTile, Tile moveToTile, int reachableDistance) {
        yield return ExecuteMoveAction (moveToTile, sourceTile, reachableDistance);
        yield return ExecuteAttackAction (targetTile, moveToTile);
    }

    private IEnumerator ExecuteAttackAction (Tile defendingTile, Tile attackingTile) {
        attackingTile.unit.hasAttacked = true;

        while (defendingTile.unit && defendingTile.unit.isBeingAttacked) {
            yield return endOfFrame;
        }

        if (!defendingTile.unit) {
            attackingTile.unit.hasAttacked = false;
        } else {
            defendingTile.unit.Hit (attackingTile.unit.attackDamage);
        }

    }

    private IEnumerator ExecuteMoveAction (Tile targetTile, Tile fromTile, int distance) {
        pathfinding.SetUnitPathDirections (fromTile.unit, targetTile);
        fromTile.unit.PlayMoveAnimation ();

        fromTile.unit.remainingMovementPoints -= distance;
        targetTile.unit = fromTile.unit;
        fromTile.unit = null;

        while (targetTile.unit && targetTile.unit.isMoving) {
            yield return endOfFrame;
        }
    }

    public void OnMouseEnterTile (Tile tile) {
        if (selectedTile && selectedTile.unit && selectedTile.unit.controllingPlayer == currentPlayer) {
            pathfinding.ShowTrail (tile);
        }
    }

    public void SwitchPlayer () {
        pathfinding.ResetPaths ();
        ResetMovementPoints ();

        currentPlayer = players[0] == currentPlayer ? players[1] : players[0];

        if (currentPlayer.isAI) {
            StartCoroutine (PlayAI ());
        } else {
            CalculatePossibleEnemyMoves ();
        }

    }

    private IEnumerator PlayAI () {

        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile tile = map.GetTile (x, y);

                if (tile.unit && tile.unit.controllingPlayer == currentPlayer) {
                    if (Camera.current && tile) {
                        Camera.current.transform.position = new Vector3 (tile.transform.position.x, tile.transform.position.y, Camera.current.transform.position.z);
                    }

                    OnTileLeftClicked (tile);
                    yield return waitSeconds;

                    OnTileRightClicked (FindClosestEnemy ());
                    yield return waitSeconds;
                }

            }
        }

        DeselectTile ();
        yield return waitSeconds;

        if (!oneActionOnly) {
            SwitchPlayer ();
        }
    }

    private Tile FindClosestEnemy () {
        Tile closest = null;
        int closestDistance = int.MaxValue;

        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile tile = map.GetTile (x, y);

                if (tile.unit && tile.unit.controllingPlayer != currentPlayer) {
                    int distance = pathfinding.GetDistanceToNode (x, y);

                    if (!closest || distance < closestDistance) {
                        closest = tile;
                        closestDistance = distance;
                    }
                }
            }
        }

        return closest;
    }

    private void CalculatePossibleEnemyMoves () {
        pathfinding.ClearPossibleEnemyMoves ();

        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile currentTile = map.GetTile (x, y);
                if (currentTile.unit && currentTile.unit.controllingPlayer != currentPlayer) {
                    pathfinding.CalculateEnemyPathsAndSetStates (currentTile);
                }
            }
        }

    }

    private void ResetMovementPoints () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile tile = map.GetTile (x, y);

                if (tile.unit != null) {
                    tile.unit.hasAttacked = false;
                    tile.unit.remainingMovementPoints = tile.unit.totalMovementPoints;
                }
            }
        }
    }

    private bool UnitsHaveActions () {
        for (int x = 0; x < map.width; x++) {
            for (int y = 0; y < map.height; y++) {
                Tile tile = map.GetTile (x, y);

                if (tile.unit != null && tile.unit.controllingPlayer == currentPlayer) {
                    // only one move and it hasn't moved (not considering a case with a static unit)
                    if (noMovingAfterTheFirstOne && !tile.unit.hasMoved) {
                        return true;
                    }

                    // has not attacked attack (not checking if enemies within range)
                    if (!tile.unit.hasAttacked) {
                        return true;
                    }

                    // isn't surrounded and has more movement points
                    if (tile.unit.remainingMovementPoints >= 5 && !IsSurrounded (x, y, tile.unit.remainingMovementPoints >= 7)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsSurrounded (int x, int y, bool includeDiagonals) {
        for (int m = -1; m <= 1; m++) {
            for (int n = -1; n <= 1; n++) {
                if (m == 0 & n == 0) {
                    continue;
                }

                if (!includeDiagonals && Mathf.Abs (m) == Mathf.Abs (n)) {
                    continue;
                }

                if (!map.GetTile (x + m, y + n).unit) {
                    return false;
                }
            }
        }

        return true;
    }
}