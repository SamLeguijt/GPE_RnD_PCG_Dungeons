using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;

public class TilemapDrawer : MonoBehaviour
{
    [SerializeField] private Tilemap roomTilemap = null;
    [SerializeField] private Tilemap corridorsTilemap = null;

    public void PaintRoomTiles(IEnumerable<Vector2Int> positions, TileBase tile)
    {
        PaintTiles(positions, roomTilemap, tile);
    }

    public void PaintCorridorTiles(IEnumerable<Vector2Int> positions, TileBase tile)
    {
        PaintTiles(positions, corridorsTilemap, tile);
    }

    private void PaintTiles(IEnumerable<Vector2Int> positions, Tilemap tilemap, TileBase tile)
    {
        foreach (var position in positions)
        {
            PaintSingleTile(tilemap, tile, position);
        }
    }

    private void PaintSingleTile(Tilemap tilemap, TileBase tile, Vector2Int position)
    {
        var tilePosition = tilemap.WorldToCell((Vector3Int)position);
        tilemap.SetTile(tilePosition, tile);
    }

    public void Clear()
    {
        corridorsTilemap.ClearAllTiles();
        roomTilemap.ClearAllTiles();
    }
}
