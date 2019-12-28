using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour {
    public Tile tilePrefab;

    public Sprite passableSprite;
    public Sprite unpassableSprite;
    public Unit[] availableUnits;

    public int width;
    public int height;

    private Tile[, ] tiles;
    private GameManager gameManager;
    private GameObject tileHolder;

    protected void Awake () {
        gameManager = GameManager.GetInstance ();

        if (!gameManager) {
            Debug.LogWarning ("No game manager found");
        }

        tileHolder = new GameObject ("Tiles");
        tileHolder.transform.position = transform.position;
        tileHolder.transform.parent = transform;

        CreateDefaultMap (tileHolder);
    }

    private void CreateDefaultMap (GameObject tileHolder) {
        width = 30;
        height = 30;
        tiles = new Tile[width, height];

        RenewTileHolder ();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Tile tile = Instantiate (tilePrefab, new Vector3 (transform.position.x + x, transform.position.y + y, 5), Quaternion.identity, tileHolder.transform);
                if ((x != 0 && x != width - 1) && (y != 0 && y != height - 1)) {
                    tile.SetWalkable (true);
                    tile.SetSprite (passableSprite);
                } else {
                    tile.SetWalkable (false);
                    tile.SetSprite (unpassableSprite);
                }

                tile.mouseEnterLeaveEvent.AddListener (gameManager.OnMouseEnterTile);

                tiles[x, y] = tile;
            }
        }
    }

    private void RenewTileHolder () {
        if (tileHolder) {
            Destroy (tileHolder);
        }

        tileHolder = new GameObject ("Tiles");
        tileHolder.transform.position = transform.position;
        tileHolder.transform.parent = transform;
    }

    protected void Update () {

    }

    public void LoadMap (string level) {
        RenewTileHolder ();

        string[] lines = level.Split (System.Environment.NewLine.ToCharArray (), System.StringSplitOptions.RemoveEmptyEntries).Reverse ().ToArray ();

        width = lines.Select (x => x.Length).Max ();
        height = lines.Length;

        tiles = new Tile[width, height];

        for (int y = 0; y < height; y++) {
            string line = lines[y];

            for (int x = 0; x < width; x++) {
                char character = line[x];
                Tile tile = Instantiate (tilePrefab, new Vector3 (transform.position.x + x, transform.position.y + y, 5), Quaternion.identity, tileHolder.transform);

                if (character == '*') {
                    tile.SetWalkable (false);
                    tile.SetSprite (unpassableSprite);
                } else {
                    tile.SetWalkable (true);
                    tile.SetSprite (passableSprite);
                }

                tile.mouseEnterLeaveEvent.AddListener (gameManager.OnMouseEnterTile);

                tiles[x, y] = tile;

                Unit unitPrefab = availableUnits.FirstOrDefault (unit => unit.type == character);
                if (unitPrefab) {
                    CreateUnit (unitPrefab, y < (height / 2) ? gameManager.players[0] : gameManager.players[1], x, y);
                }
            }
        }

    }

    public bool IsWalkable (int x, int y) {
        return tiles[x, y].IsWalkable ();
    }

    public bool IsOccupied (int x, int y) {
        if (IsWithinBounds (x, y)) {
            return tiles[x, y].IsOccupied ();
        } else {
            return true;
        }
    }

    public bool IsFree (int x, int y) {
        return !IsOccupied (x, y);
    }

    public void AddUnit (int x, int y, Unit unit) {
        tiles[x, y].unit = unit;
    }

    public Tile GetTile (int x, int y) {
        return tiles[x, y];
    }

    public bool IsWithinBounds (int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public void CreateUnit (Unit prefab, Player player, int x, int y) {
        Unit unit = Instantiate (prefab, new Vector3 (x, y), Quaternion.identity, transform);
        unit.SetControllingPlayer (player);
        AddUnit (x, y, unit);
    }
}