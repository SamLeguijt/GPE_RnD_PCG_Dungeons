using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WaveCollapse;
using Random = UnityEngine.Random;

public class WaveCollapseGenerator : AbstractGenerator
{
    [Header("References")]
    [SerializeField] private GameObject tileCellObject = null;
    [SerializeField] private TilemapDrawer tilemapDrawer;
    [SerializeField] private TileData[] snowTiles;

    [Header("Generate settings")]
    [SerializeField] private Vector2Int gridDimensions = Vector2Int.one;
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private bool delayedGeneration = false;

    private List<TileCell> gridCells = new List<TileCell>();
    private Coroutine delayedWaveCollapseCoroutine = null;

    private int iterations = 0;
    private static GameObject tilesParent = null;

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

        tilesParent = new GameObject("TileCellParent");
        Transform parent = tilesParent.transform;

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                TileCell tileCell = Instantiate(tileCellObject, new Vector2(x, y), Quaternion.identity, parent).GetComponent<TileCell>();
                tileCell.Create(snowTiles, false, ThemesEnum.Snow);
                gridCells.Add(tileCell);
            }
        }

        StartCoroutine(CheckEntropy());
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
                    Debug.Log("Called");
                    newGenerationCell[index] = gridCells[index];
                }
                else
                {
                    List<TileData> options = new List<TileData>();

                    foreach (TileData tile in snowTiles)
                    {
                        options.Add(tile);
                    }

                    if (y > 0)
                    {

                        TileCell up = gridCells[x + (y - 1) * gridDimensions.x];

                        List<TileData> validOptions = new List<TileData>();

                        foreach (TileData possibleOptions in up.tileOptions)
                        {
                            var validOption = Array.FindIndex(snowTiles, obj => obj == possibleOptions);
                            var valid = snowTiles[validOption].northNeighbours;

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
                            var validOption = Array.FindIndex(snowTiles, obj => obj == possibleOptions);
                            var valid = snowTiles[validOption].westNeighbours;

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
                            var validOption = Array.FindIndex(snowTiles, obj => obj == possibleOptions);
                            var valid = snowTiles[validOption].southNeighbours;

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
                            var validOption = Array.FindIndex(snowTiles, obj => obj == possibleOptions);
                            var valid = snowTiles[validOption].eastNeighbours;

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
