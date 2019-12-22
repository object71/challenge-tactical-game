using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public Tile tilePrefab;

    public Sprite passableSprite;
    public Sprite unpassableSprite;

    public int width = 30;
    public int height = 30;

    private Tile[,] tiles;
    private GameManager gameManager;

    protected void Awake()
    {
        gameManager = GameManager.GetInstance();

        if (!gameManager)
        {
            Debug.LogWarning("No game manager found");
        }

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = Instantiate(tilePrefab, new Vector3(transform.position.x + x, transform.position.y + y, 5), Quaternion.identity, transform);
                if ((x != 0 && x != width - 1) && (y != 0 && y != height - 1))
                {
                    tile.SetWalkable(true);
                    tile.SetSprite(passableSprite);
                }
                else
                {
                    tile.SetWalkable(false);
                    tile.SetSprite(unpassableSprite);
                }

                tile.mouseEnterLeaveEvent.AddListener(gameManager.OnMouseEnterTile);

                tiles[x, y] = tile;
            }
        }
    }

    protected void Update()
    {

    }

    public bool IsWalkable(int x, int y)
    {
        return tiles[x, y].IsWalkable();
    }

    public bool IsOccupied(int x, int y)
    {
        if (IsWithinBounds(x, y))
        {
            return tiles[x, y].IsOccupied();
        }
        else
        {
            return true;
        }
    }

    public bool IsFree(int x, int y)
    {
        return !IsOccupied(x, y);
    }

    public void AddUnit(int x, int y, Unit unit)
    {
        tiles[x, y].unit = unit;
    }

    public Tile GetTile(int x, int y)
    {
        return tiles[x, y];
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}