using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using WaveCollapse;
using Random = UnityEngine.Random;

public class WaveCollapseGenerator : AbstractGenerator
{
    public static WaveCollapseGenerator Instance;

    [Header("References")]
    [SerializeField] private GameObject tileCellObject = null;
    [SerializeField] private TilemapDrawer tilemapDrawer;
    [SerializeField] private TileData[] allPossibleTiles;

    [Header("Generate settings")]
    [SerializeField] private Vector2Int gridDimensions = Vector2Int.one;
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private bool delayedGeneration = false;

    private List<TileCell> gridCells = new List<TileCell>();
    private Coroutine delayedWaveCollapseCoroutine = null;

    private int iterations = 0;
    private static GameObject tilesParent = null;

    List<Vector2Int> toCollapseList = new List<Vector2Int>();

    TileData[,] tileGrid;

    private void OnValidate()
    {
        if (Instance != null)
            Instance = null;

        Instance = this;
    }

    private void StartWaveCollapse()
    {
        if (delayedGeneration)
            delayedWaveCollapseCoroutine = StartCoroutine(WaveCollapseCoroutine());
        else
        {
            ExecuteWaveCollapse();
        }
    }

    private void ExecuteWaveCollapse()
    {
        gridCells.Clear();
        CreateGrid();
    }

    private IEnumerator WaveCollapseCoroutine()
    {
        yield return null;
    }

    private void CreateGrid()
    {
        if (tilesParent != null)
            DestroyImmediate(tilesParent);

        tileGrid = new TileData[gridDimensions.x, gridDimensions.y];
        List<Vector2Int> cellsToCollapse = new();

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                cellsToCollapse.Add(new Vector2Int(x, y));
            }
        }

        ExecuteWaveCollapseAlgorithm(cellsToCollapse);
    }

    public void CollapseRoom(Room room)
    {
        if (room.RoomTheme != ThemesEnum.Snow)
            return;

        List<Vector2Int> cellsToCollapse = new List<Vector2Int>(room.RoomPositions);

        ExecuteWaveCollapseAlgorithm(cellsToCollapse);
    }

    public void ExecuteWaveCollapseAlgorithm(List<Vector2Int> cellsToCollapse)
    {
        Dictionary<Vector2Int, TileData> positionsTilesDict = new();

        // Iterate over room positions and assign tile data based on neighborhood compatibility
        foreach (Vector2Int position in cellsToCollapse)
        {
            if (positionsTilesDict.ContainsKey(position))
                continue;

            // Make a fresh copy of all possible tiles
            List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

            int x = position.x;
            int y = position.y;

            for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
            {
                Vector2Int currentDirection = Direction2D.cardinalDirectionsList[i];
                Vector2Int neighbour = new Vector2Int(x + currentDirection.x, y + currentDirection.y);

                if (!positionsTilesDict.TryGetValue(neighbour, out TileData neighbourTileData))
                    continue; 

                switch (i)
                {
                    case 0: // North
                        possibleTiles = FilterPossibleTiles(possibleTiles, neighbourTileData.southNeighbours);
                        break;
                    case 1: // East
                        possibleTiles = FilterPossibleTiles(possibleTiles, neighbourTileData.westNeighbours);
                        break;
                    case 2: // South
                        possibleTiles = FilterPossibleTiles(possibleTiles, neighbourTileData.northNeighbours);
                        break;
                    case 3: // West
                        possibleTiles = FilterPossibleTiles(possibleTiles, neighbourTileData.eastNeighbours);
                        break;
                }
            }

            if (possibleTiles.Count > 0)
            {
                TileData tileData = possibleTiles[Random.Range(0, possibleTiles.Count)];
                positionsTilesDict.Add(position, tileData);
            }
            else
            {
                Debug.LogWarning("No possible tile for tile at position: " + position);
            }
        }

        foreach (KeyValuePair<Vector2Int, TileData> kvp in positionsTilesDict)
        {
            Vector2Int position = kvp.Key;
            TileData tileData = kvp.Value;

            tilemapDrawer.PaintWaveCollapseTile(position, tileData.tileSprite);
        }
    }


    private List<TileData> FilterPossibleTiles(List<TileData> possibleTiles, List<TileData> validOptions)
    {
        for (int i = possibleTiles.Count - 1; i >= 0; i--)
        {
            var element = possibleTiles[i];
            if (!validOptions.Contains(element))
            {
                possibleTiles.RemoveAt(i);
            }
        }

        return possibleTiles;
    }

    public override void OnGenerate()
    {
        StartWaveCollapse();
    }

    public override void OnClear()
    {
        StopAllCoroutines();

        if (delayedWaveCollapseCoroutine != null)
            delayedWaveCollapseCoroutine = null;

        if (tilesParent != null)
            DestroyImmediate(tilesParent);

        tilemapDrawer.Clear(tilemapDrawer.WfcTilemap);
    }
}
