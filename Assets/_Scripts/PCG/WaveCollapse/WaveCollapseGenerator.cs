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
        //tilesParent = new GameObject("TileCellParent");
        //Transform parent = tilesParent.transform;

        if (room.RoomTheme != ThemesEnum.Snow)
        {
            Debug.Log("Room is not a snow theme, returning");
            return;
        }

        TileData[,] tileDataGrid = new TileData[room.roomBounds.size.x, room.roomBounds.size.y];
        List<Vector2Int> cellsToCollapse = new List<Vector2Int>(room.RoomPositions);

        CollapseWave(tileDataGrid, cellsToCollapse);
    }

    private IEnumerator RunWaveCollapseUntilComplete()
    {
        int max = 10000000;
        int current = 0;
        List<Vector2Int> toCollapse = new();
        while (HasEmptyTiles() || current < max)
        {
            current++;
            //CollapseWave(toCollapse);

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

    private bool IsInsideRoomPositions(Vector2Int position, List<Vector2Int> roomPositions)
    {
        return roomPositions.Contains(position);
    }

    private void CollapseWave(TileData[,] roomGrid, List<Vector2Int> roomPositions)
    {
        //if (roomPositions.Count == 0)
        //roomPositions.Add(new Vector2Int(gridDimensions.x / 2, gridDimensions.y / 2));

        int x = roomPositions[0].x;
        int y = roomPositions[0].y;

        List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

        // TODO: Fix all room positions not being inside the roomgrid.
        foreach (Vector2Int position in roomPositions)
        {
            if (!IsInsideGrid(roomGrid, position.x, position.y))
                Debug.Log("room pos is not inside grid.");
        }

        for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
        {
            Vector2Int currentDirection = Direction2D.cardinalDirectionsList[i];
            Vector2Int neighbour = new Vector2Int(x + currentDirection.x, y + currentDirection.y);

            // Note: Previous code.
            //if (!IsInsideGrid(neighbour.x, neighbour.y))
            //    continue;

            if (!IsInsideRoomPositions(neighbour, roomPositions))
            {
                Debug.Log("Neighbour is not inside room positions, continuing to next direction. Neighbour: " + neighbour);
                continue;
            }

            if (!IsInsideGrid(roomGrid, neighbour.x, neighbour.y))
                continue;

            TileData neighbourTileData = roomGrid[neighbour.x, neighbour.y];

            //TileData neighbourTileData = toCollapse[toCollapse.IndexOf(neighbour)];

            if (neighbourTileData == null)
            {
                if (!roomPositions.Contains(neighbour))
                    roomPositions.Add(neighbour);
            }
            else
            {
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
        }

        if (possibleTiles.Count > 0)
        {
            // TODO: Apply weights here.
            TileData tile = possibleTiles[Random.Range(0, possibleTiles.Count)];
            roomGrid[x, y] = tile;

            tilemapDrawer.PaintWaveCollapseTile(new Vector2Int(x, y), tile.tileSprite);
            toCollapseList.RemoveAt(0);
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

    private bool IsInsideGrid(TileData[,] grid, int x, int y)
    {
        return x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1);
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
