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
    public bool isUnitWithinRange;
    public bool isWithinAttackRange;

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

    public void SetIsUnitWithinRange(bool value)
    {
        isUnitWithinRange = value;
        UpdateColorState();
    }

    public void SetIsWithinEnemyMoveRange(bool value)
    {
        isWithinEnemyMoveRange = value;
        UpdateColorState();
    }

    public void SetIsWithinAttackRange(bool value)
    {
        isWithinAttackRange = value;
        UpdateColorState();
    }

    public void SetIsCurrentPath(bool value)
    {
        isCurrentPath = value;
        UpdateColorState();
    }

    public void UpdateColorState()
    {
        if (isSelected)
        {
            tileSpriteRenderer.color = new Color32(102, 153, 255, 255);
        }
        else if (unit)
        {
            if (unit.controllingPlayer != GameManager.GetInstance().currentPlayer)
            {
                if (isUnitWithinRange)
                {
                    if (isHoveredOver)
                    {
                        tileSpriteRenderer.color = new Color32(128, 0, 0, 255);
                    }
                    else
                    {
                        tileSpriteRenderer.color = new Color32(153, 51, 51, 255);
                    }
                }
                else
                {
                    tileSpriteRenderer.color = new Color32(255, 153, 102, 255);
                }
            }
            else
            {
                tileSpriteRenderer.color = new Color32(153, 255, 102, 255);
            }
        }
        else if (isInMoveRange)
        {
            if (isCurrentPath && isHoveredOver)
            {
                tileSpriteRenderer.color = new Color32(0, 153, 255, 255);
            }
            else if (isCurrentPath)
            {
                tileSpriteRenderer.color = new Color32(51, 204, 255, 255);
            }
            else if (isWithinEnemyMoveRange)
            {
                if (isWithinAttackRange)
                {
                    tileSpriteRenderer.color = new Color32(102, 102, 153, 255);
                }
                else
                {
                    tileSpriteRenderer.color = new Color32(153, 153, 255, 255);
                }
            }
            else if (isWithinAttackRange)
            {
                tileSpriteRenderer.color = new Color32(51, 102, 153, 255);
            }
            else
            {
                tileSpriteRenderer.color = new Color32(102, 204, 255, 255);
            }
        }
        else if (isHoveredOver && isSelected)
        {
            // None if it is selected there is no need for coloring on hover
        }
        else if (isHoveredOver)
        {
            tileSpriteRenderer.color = new Color32(204, 255, 255, 255);
        }
        else if (GameManager.GetInstance().selectedTile && isWithinEnemyMoveRange)
        {
            tileSpriteRenderer.color = new Color32(255, 204, 255, 255);
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
