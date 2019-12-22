using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int totalMovementPoints;
    public int totalHealthPoints;
    public int attackRange;
    public int attackDamage;
    public bool hasAttacked;
    public bool isMoving = false;

    public int remainingHealthPoints;
    public int remainingMovementPoints;

    public Player controllingPlayer;

    private SpriteRenderer rendererComponent;
    private GameManager gameManager;
    private List<Vector3> movementPath;
    private int totalPathDistance;
    private float movementAnimationPerNode = 0.25f;
    private static WaitForSeconds halfSecondWait = new WaitForSeconds(0.5f);
    private static WaitForEndOfFrame frameWait = new WaitForEndOfFrame();

    // Start is called before the first frame update
    void Awake()
    {
        gameManager = GameManager.GetInstance();
        movementPath = new List<Vector3>(totalMovementPoints / 5);
        rendererComponent = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetControllingPlayer(Player player)
    {
        controllingPlayer = player;
        rendererComponent.color = controllingPlayer.color;
    }

    public void SetIsSelected(bool value)
    {
        rendererComponent.color = value ? controllingPlayer.selectedUnitColor : controllingPlayer.color;
    }

    public void MoveToTile(Tile targetTile, PathfindingNode[,] graph)
    {
        Vector2Int position = targetTile.GetMapPosition();

        PathfindingNode node = graph[position.x, position.y];
        while (node.parent != null)
        {
            movementPath.Add(node.tile.transform.position);
            node = node.parent;
        }

        movementPath.Reverse();


        StartCoroutine("MoveByPath");
    }

    public void PlayBlinkAnimation()
    {
        StartCoroutine("Blink");
    }

    private IEnumerator Blink()
    {
        for (int i = 0; i < 4; i++)
        {
            rendererComponent.enabled = !rendererComponent.enabled;
            yield return halfSecondWait;
        }

        rendererComponent.enabled = true;
    }

    private IEnumerator MoveByPath()
    {
        isMoving = true;

        foreach (Vector3 target in movementPath)
        {
            float movementTime = Time.deltaTime / movementAnimationPerNode;

            float deltaDistanceX = target.x - transform.position.x;
            float moveXPerFrame = deltaDistanceX * movementTime;

            float deltaDistanceY = target.y - transform.position.y;
            float moveYPerFrame = deltaDistanceY * movementTime;

            while (Mathf.Abs(target.x - transform.position.x) >= 0.1f || Mathf.Abs(target.y - transform.position.y) >= 0.1f)
            {
                transform.Translate(moveXPerFrame, moveYPerFrame, 0f);
                yield return frameWait;
            }

        }

        transform.position = Vector3Int.RoundToInt(transform.position);
        isMoving = false;
        movementPath.Clear();
    }
}
