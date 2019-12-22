using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Unit unit;
    public TileMouseEnterEvent mouseEnterLeaveEvent = new TileMouseEnterEvent();

    private SpriteRenderer tileSpriteRenderer;
    private bool isWalkable;
    private bool isHoveredOver;
    private bool isSelected;
    public bool isInMoveRange;
    public bool isWithinEnemyMoveRange;
    public bool isCurrentPath;
    public bool isEnemyWithinRange;

    // Start is called before the first frame update
    void Awake()
    {
        tileSpriteRenderer = GetComponent<SpriteRenderer>();

        if (!tileSpriteRenderer)
        {
            Debug.LogWarning("Tile has no sprite renderer");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnMouseEnter()
    {
        mouseEnterLeaveEvent.Invoke(this);

        isHoveredOver = true;
        UpdateColorState();
    }

    void OnMouseExit()
    {
        isHoveredOver = false;
        UpdateColorState();
    }

    public void SetSprite(Sprite sprite)
    {
        tileSpriteRenderer.sprite = sprite;
    }

    public bool IsWalkable()
    {
        return isWalkable;
    }

    public bool IsFree()
    {
        if (unit == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsOccupied()
    {
        return !IsFree();
    }

    public void SetWalkable(bool value)
    {
        isWalkable = value;
    }

    public void SetIsInMoveRange(bool value)
    {
        isInMoveRange = value;
        UpdateColorState();
    }

    public void SetIsEnemyWithinRange(bool value)
    {
        isEnemyWithinRange = value;
        UpdateColorState();
    }

    public void SetIsWithinEnemyMoveRange(bool value)
    {
        isWithinEnemyMoveRange = value;
        UpdateColorState();
    }

    public void SetIsCurrentPath(bool value)
    {
        isCurrentPath = value;
        UpdateColorState();
    }

    public void UpdateColorState()
    {
        if (isHoveredOver && isSelected)
        {
            tileSpriteRenderer.color = Color.green;
        }
        else if (isHoveredOver)
        {
            tileSpriteRenderer.color = Color.yellow;
        }
        else if (isSelected)
        {
            tileSpriteRenderer.color = Color.cyan;
        }
        else if (isInMoveRange)
        {
            if (isCurrentPath)
            {
                tileSpriteRenderer.color = Color.gray;
            }
            else
            {
                tileSpriteRenderer.color = Color.magenta;
            }
        }
        else if (isEnemyWithinRange)
        {
            tileSpriteRenderer.color = Color.red;
        }
        else
        {
            tileSpriteRenderer.color = Color.white;
        }
    }

    public void SetIsSelected(bool value)
    {
        isSelected = value;

        if (unit)
        {
            unit.SetIsSelected(value);
        }

        UpdateColorState();
    }

    public Vector2Int GetMapPosition()
    {
        return new Vector2Int((int)transform.position.x, (int)transform.position.y);
    }
}
