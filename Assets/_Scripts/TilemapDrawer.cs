using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;

public class TilemapDrawer : MonoBehaviour
{
    [field: SerializeField] public Tilemap RoomTilemap { get; private set; }
    [field: SerializeField] public Tilemap CorridorsTilemap { get; private set; }
    [field: SerializeField] public Tilemap WfcTilemap { get; private set; }

    public void PaintRoomTiles(IEnumerable<Vector2Int> positions, TileBase tile)
    {
        PaintTiles(positions, RoomTilemap, tile);
    }

    public void PaintCorridorTiles(IEnumerable<Vector2Int> positions, TileBase tile)
    {
        PaintTiles(positions, CorridorsTilemap, tile);
    }

    public void PaintWaveCollapseTile(Vector2Int position, TileBase tile)
    {
        PaintSingleTile(WfcTilemap, tile, position);
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
    
    public void Clear(Tilemap mapToClear)
    {
        mapToClear.ClearAllTiles();
    }
}
