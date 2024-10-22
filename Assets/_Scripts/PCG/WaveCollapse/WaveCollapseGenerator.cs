using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using WaveCollapse;
using Random = UnityEngine.Random;

public class WaveCollapseGenerator : AbstractGenerator
{
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
        iterations = 0;
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

        tilesParent = new GameObject("TileCellParent");
        Transform parent = tilesParent.transform;

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                TileCell tileCell = Instantiate(tileCellObject, new Vector2(x, y), Quaternion.identity, parent).GetComponent<TileCell>();
                tileCell.Create(allPossibleTiles, false, ThemesEnum.Snow);
                gridCells.Add(tileCell);
            }
        }

        tileGrid = new TileData[gridDimensions.x, gridDimensions.y];

        StartCoroutine(RunWaveCollapseUntilComplete());
    }

    public void CollapseRoom(Room room)
    {
        tilesParent = new GameObject("TileCellParent");
        Transform parent = tilesParent.transform;

        foreach (Vector2Int position in room.RoomPositions)
        {
            // Add all positions to the grid. 

            // 
        }
    }

    private IEnumerator RunWaveCollapseUntilComplete()
    {
        int max = 10000000;
        int current = 0;
        List<Vector2Int> toCollapse = new();

        while (HasEmptyTiles() || current < max)
        {
            current++;
            CollapseWave(toCollapse);

            if (!HasEmptyTiles())
                break;

            yield return delay;
        }
    }

    private bool HasEmptyTiles()
    {
        foreach (var tile in tileGrid)
        {
            if (tile == null)
            {
                return true;
            }
        }
        return false;
    }

    private void CollapseWave(List<Vector2Int> toCollapse)
    {
        if (toCollapse.Count == 0)
            toCollapse.Add(new Vector2Int(gridDimensions.x / 2, gridDimensions.y / 2));

        int x = toCollapse[0].x;
        int y = toCollapse[0].y;

        List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

        for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
        {
            Vector2Int currentDirection = Direction2D.cardinalDirectionsList[i];
            Vector2Int neighbour = new Vector2Int(x + currentDirection.x, y + currentDirection.y);

            if (!IsInsideGrid(neighbour.x, neighbour.y))
                continue;

            TileData neighbourTileData = tileGrid[neighbour.x, neighbour.y];

            if (neighbourTileData != null)
            {
                Debug.Log("Neighbourtiledata not null");

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
            else
            {
                if (!toCollapse.Contains(neighbour))
                    toCollapse.Add(neighbour);
            }
        }

        if (possibleTiles.Count < 1)
        {
            tileGrid[x, y] = null; // Mark it as empty for re-processing
            Debug.Log("No potential tiles for: " + x + ", " + y);
        }
        else
        {
            TileData tile = possibleTiles[Random.Range(0, possibleTiles.Count)];
            tileGrid[x, y] = tile;
            tilemapDrawer.PaintWaveCollapseTile(new Vector2Int(x, y), tile.tileSprite);
        }

        toCollapse.RemoveAt(0);
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

    private void CollapseWorldInitial()
    {
        toCollapseList.Clear();

        toCollapseList.Add(new Vector2Int(gridDimensions.x / 2, gridDimensions.y / 2));

        while (toCollapseList.Count > 0)
        {
            int x = toCollapseList[0].x;
            int y = toCollapseList[0].y;

            List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

            for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
            {
                Vector2Int direction = Direction2D.cardinalDirectionsList[i];
                Vector2Int neighbour = new Vector2Int(x + direction.x, y + direction.y);

                // TODO: Check if neighbour is inside grid here...
                if (!IsInsideGrid(neighbour.x, neighbour.y))
                    continue;

                TileData neighbourTileData = tileGrid[neighbour.x, neighbour.y];

                if (neighbourTileData != null)
                {
                    switch (i)
                    {
                        case 0:
                            FilterPossibleTiles(possibleTiles, neighbourTileData.southNeighbours);
                            break;
                        case 1:
                            FilterPossibleTiles(possibleTiles, neighbourTileData.westNeighbours);
                            break;
                        case 2:
                            FilterPossibleTiles(possibleTiles, neighbourTileData.northNeighbours);
                            break;
                        case 3:
                            FilterPossibleTiles(possibleTiles, neighbourTileData.eastNeighbours);
                            break;
                    }
                }
                else
                {
                    if (!toCollapseList.Contains(neighbour))
                        toCollapseList.Add(neighbour);
                }
            }

            TileData tile;

            if (possibleTiles.Count < 1)
            {
                //tileGrid[x, y] = allPossibleTiles[0];
                //tile = tileGrid[x, y];
                Debug.Log("No potential tiles for :" + x + y);
            }
            else
            {
                tile = possibleTiles[Random.Range(0, possibleTiles.Count)];
                tileGrid[x, y] = tile;
                tilemapDrawer.PaintWaveCollapseTile(new Vector2Int(x, y), tile.tileSprite);
                toCollapseList.RemoveAt(0);
            }

        }
    }

    private bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < gridDimensions.x && y >= 0 && y < gridDimensions.y;
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
