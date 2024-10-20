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

        //StartCoroutine(CheckEntropy());


        tileGrid = new TileData[gridDimensions.x, gridDimensions.y];

        //CollapseWorld();
        StartCoroutine(RunWaveCollapseUntilComplete());

        //CollapseWorldRecursive();
    }

    private IEnumerator CheckEntropy()
    {
        List<TileCell> tempGrid = new List<TileCell>(gridCells);

        tempGrid.RemoveAll(c => c.IsCollapsed);
        tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

        int arrLength = tempGrid[0].tileOptions.Length;
        int stopIndex = default;

        for (int i = 0; i < tempGrid.Count; i++)
        {
            if (tempGrid[i].tileOptions.Length > arrLength)
            {
                stopIndex = i;
                break;
            }
        }

        if (stopIndex > 0)
        {
            tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
        }

        yield return new WaitForSeconds(delay);

        CollapseCell(tempGrid);

    }

    private void CollapseWorldRecursive()
    {
        if (toCollapseList.Count == 0)
        {
            // Initialize starting point if the list is empty
            toCollapseList.Add(new Vector2Int(gridDimensions.x / 2, gridDimensions.y / 2));
        }

        if (toCollapseList.Count == 0) return; // Base case: No more tiles to collapse

        int x = toCollapseList[0].x;
        int y = toCollapseList[0].y;

        List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

        for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
        {
            Vector2Int direction = Direction2D.cardinalDirectionsList[i];
            Vector2Int neighbour = new Vector2Int(x + direction.x, y + direction.y);

            // Ensure the neighbour is inside the grid
            if (!IsInsideGrid(neighbour.x, neighbour.y))
                continue;

            TileData neighbourTileData = tileGrid[neighbour.x, neighbour.y];

            if (neighbourTileData != null)
            {
                switch (i)
                {
                    case 0: // North
                        CheckValidity(possibleTiles, neighbourTileData.southNeighbours);
                        break;
                    case 1: // East
                        CheckValidity(possibleTiles, neighbourTileData.westNeighbours);
                        break;
                    case 2: // South
                        CheckValidity(possibleTiles, neighbourTileData.northNeighbours);
                        break;
                    case 3: // West
                        CheckValidity(possibleTiles, neighbourTileData.eastNeighbours);
                        break;
                }
            }
            else
            {
                if (!toCollapseList.Contains(neighbour))
                    toCollapseList.Add(neighbour);
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

        toCollapseList.RemoveAt(0);
        CollapseWorldRecursive(); // Recursively process the next tile
    }

    public void CollapseCell(List<TileCell> cells)
    {
        int randomCell = Random.Range(0, cells.Count);

        TileCell toCollapse = cells[randomCell];

        toCollapse.IsCollapsed = true;

        TileData selectedTile = toCollapse.tileOptions[Random.Range(0, toCollapse.tileOptions.Length)];
        toCollapse.tileOptions = new TileData[] { selectedTile };

        TileData foundTile = toCollapse.tileOptions[0];
        tilemapDrawer.PaintWaveCollapseTile(new Vector2Int((int)toCollapse.transform.position.x, (int)toCollapse.transform.position.y), foundTile.tileSprite);
        UpdateGeneration();

    }

    private void UpdateGeneration()
    {
        List<TileCell> newGenerationCell = new List<TileCell>(gridCells);

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                var index = x + y * gridDimensions.x;

                if (gridCells[index].IsCollapsed)
                {
                    newGenerationCell[index] = gridCells[index];
                }
                else
                {
                    List<TileData> options = new List<TileData>();

                    foreach (TileData tile in allPossibleTiles)
                    {
                        options.Add(tile);
                    }

                    if (y > 0)
                    {

                        TileCell up = gridCells[x + (y - 1) * gridDimensions.x];

                        List<TileData> validOptions = new List<TileData>();

                        foreach (TileData possibleOptions in up.tileOptions)
                        {
                            var validOption = Array.FindIndex(allPossibleTiles, obj => obj == possibleOptions);
                            var valid = allPossibleTiles[validOption].northNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x < gridDimensions.x - 1)
                    {
                        TileCell right = gridCells[x + 1 + y * gridDimensions.x].GetComponent<TileCell>();

                        List<TileData> validOptions = new List<TileData>();

                        foreach (TileData possibleOptions in right.tileOptions)
                        {
                            var validOption = Array.FindIndex(allPossibleTiles, obj => obj == possibleOptions);
                            var valid = allPossibleTiles[validOption].westNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (y < gridDimensions.y - 1)
                    {
                        TileCell down = gridCells[x + (y + 1) * gridDimensions.x].GetComponent<TileCell>();
                        List<TileData> validOptions = new List<TileData>();

                        foreach (TileData possibleOptions in down.tileOptions)
                        {
                            var validOption = Array.FindIndex(allPossibleTiles, obj => obj == possibleOptions);
                            var valid = allPossibleTiles[validOption].southNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x > 0)
                    {
                        TileCell left = gridCells[x - 1 + y * gridDimensions.x].GetComponent<TileCell>();
                        List<TileData> validOptions = new List<TileData>();

                        foreach (TileData possibleOptions in left.tileOptions)
                        {
                            var validOption = Array.FindIndex(allPossibleTiles, obj => obj == possibleOptions);
                            var valid = allPossibleTiles[validOption].eastNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    TileData[] newTiles = new TileData[options.Count];

                    for (int i = 0; i < options.Count; i++)
                    {
                        newTiles[i] = options[i];
                    }

                    newGenerationCell[index].Recreate(newTiles);
                }
            }
        }

        gridCells = newGenerationCell;
        iterations++;

        if (iterations < gridDimensions.x * gridDimensions.y)
        {
            StartCoroutine(CheckEntropy());
        }
    }

    private void CheckValidity(List<TileData> tileOptions, List<TileData> validOption)
    {
        for (int i = tileOptions.Count - 1; i >= 0; i--)
        {
            var element = tileOptions[i];
            if (!validOption.Contains(element))
            {
                tileOptions.RemoveAt(i);
            }
        }
    }

    private IEnumerator RunWaveCollapseUntilComplete()
    {
        int max = 10000000;
        int current = 0;

        while (HasEmptyTiles() || current < max)
        {
            current++;
            Debug.Log(current);
            CollapseWorld();

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

    private void CollapseWorld()
    {
        if (toCollapseList.Count == 0)
        {
            // Initialize starting point if the list is empty
            toCollapseList.Add(new Vector2Int(gridDimensions.x / 2, gridDimensions.y / 2));
        }

        int x = toCollapseList[0].x;
        int y = toCollapseList[0].y;

        List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

        for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
        {
            Vector2Int direction = Direction2D.cardinalDirectionsList[i];
            Vector2Int neighbour = new Vector2Int(x + direction.x, y + direction.y);

            // Ensure the neighbour is inside the grid
            if (!IsInsideGrid(neighbour.x, neighbour.y))
                continue;

            TileData neighbourTileData = tileGrid[neighbour.x, neighbour.y];

            if (neighbourTileData != null)
            {
                switch (i)
                {
                    case 0: // North
                        CheckValidity(possibleTiles, neighbourTileData.southNeighbours);
                        break;
                    case 1: // East
                        CheckValidity(possibleTiles, neighbourTileData.westNeighbours);
                        break;
                    case 2: // South
                        CheckValidity(possibleTiles, neighbourTileData.northNeighbours);
                        break;
                    case 3: // West
                        CheckValidity(possibleTiles, neighbourTileData.eastNeighbours);
                        break;
                }
            }
            else
            {
                if (!toCollapseList.Contains(neighbour))
                    toCollapseList.Add(neighbour);
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

        toCollapseList.RemoveAt(0);
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
                            CheckValidity(possibleTiles, neighbourTileData.southNeighbours);
                            break;
                        case 1:
                            CheckValidity(possibleTiles, neighbourTileData.westNeighbours);
                            break;
                        case 2:
                            CheckValidity(possibleTiles, neighbourTileData.northNeighbours);
                            break;
                        case 3:
                            CheckValidity(possibleTiles, neighbourTileData.eastNeighbours);
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
                tileGrid[x, y] = allPossibleTiles[0];
                tile = tileGrid[x, y];
                Debug.Log("No potential tiles for :" + x + y);
            }
            else
            {
                tile = possibleTiles[Random.Range(0, possibleTiles.Count)];
                tileGrid[x, y] = tile;
            }

            tilemapDrawer.PaintWaveCollapseTile(new Vector2Int(x, y), tile.tileSprite);
            toCollapseList.RemoveAt(0);
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
