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
    Dictionary<Vector2Int, TileData> positionsTilesDict = new();

    TileData[,] tileGrid;

    private void OnValidate()
    {
        if (Instance != null)
            Instance = null;

        Instance = this;
    }

    public override void OnGenerate()
    {
        CreateGridBasedWFC();
    }

    public override void OnClear()
    {
        StopAllCoroutines();

        if (delayedWaveCollapseCoroutine != null)
            delayedWaveCollapseCoroutine = null;

        if (tilesParent != null)
            DestroyImmediate(tilesParent);

        positionsTilesDict.Clear();
        tilemapDrawer.Clear(tilemapDrawer.RoomTilemap);
    }

    private void CreateGridBasedWFC()
    {
        gridCells.Clear();
        CreateGrid();
    }

    private void CreateGrid()
    {
        if (tilesParent != null)
            DestroyImmediate(tilesParent);

        List<Vector2Int> cellsToCollapse = new();

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                cellsToCollapse.Add(new Vector2Int(x, y));
            }
        }

        StartCoroutine(ExecuteWaveCollapseAlgorithmEntropied(cellsToCollapse));
    }

    public void CollapseRoom(Room room)
    {
        // TODO: Tile selection based on theme.
        //if (room.RoomTheme != ThemesEnum.Snow)
        //    return;

        List<Vector2Int> cellsToCollapse = new List<Vector2Int>(room.RoomPositions);

        //ExecuteWaveFunctionCollapseAlgorithm(cellsToCollapse);
        StartCoroutine(ExecuteWaveCollapseAlgorithmEntropied(cellsToCollapse));

    }

    public void ExecuteWaveFunctionCollapseAlgorithm(List<Vector2Int> cellsToCollapse)
    {
        Dictionary<Vector2Int, TileData> positionsTilesDict = new();

        foreach (Vector2Int cellPosition in cellsToCollapse)
        {
            // Skip already collapsed tiles. 
            if (positionsTilesDict.ContainsKey(cellPosition))
                continue;

            List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

            int x = cellPosition.x;
            int y = cellPosition.y;

            // Check in NESW directions for a neighbouring tile and filter possible tiles based on them.
            for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
            {
                Vector2Int currentDirection = Direction2D.cardinalDirectionsList[i];
                Vector2Int neighbour = new Vector2Int(x + currentDirection.x, y + currentDirection.y);

                // Skip if neighbour is not one of the cells to collapse.
                if (!cellsToCollapse.Contains(neighbour))
                    continue;

                // Get the TileData of the neighbour to filter possible tiles.
                if (!positionsTilesDict.TryGetValue(neighbour, out TileData neighbourTileData))
                    continue; 

                // Filter possible tiles based on the current direction and their allowed nighbours on the opposite direction.
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

            // Add a random based tile data from the possibilities with the current cell to the dict.
            if (possibleTiles.Count > 0)
            {
                TileData tileData = possibleTiles[Random.Range(0, possibleTiles.Count)];
                positionsTilesDict.Add(cellPosition, tileData);
            }
            else
            {
                Debug.LogWarning("No possible tile for tile at position: " + cellPosition);
            }
        }

        // Paint the tile on each cell position.
        foreach (KeyValuePair<Vector2Int, TileData> kvp in positionsTilesDict)
        {
            Vector2Int position = kvp.Key;
            TileData tileData = kvp.Value;

            tilemapDrawer.PaintWaveCollapseTile(position, tileData.tileSprite);
        }
    }

    public IEnumerator ExecuteWaveCollapseAlgorithmEntropied(List<Vector2Int> cellsToCollapse)
    {
        WaitForSeconds delay = new WaitForSeconds(this.delay);
        Dictionary<Vector2Int, int> entropyDict = new();

        // Initialize entropy for each cell based on all possible tiles.
        foreach (Vector2Int cellPosition in cellsToCollapse)
        {
            entropyDict[cellPosition] = allPossibleTiles.Length; // Maximum possible tiles initially
        }

        // Run until all cells are collapsed
        while (cellsToCollapse.Count > 0)
        {
            // Select the cell with the lowest entropy
            Vector2Int cellToCollapse = GetCellWithLowestEntropy(cellsToCollapse, entropyDict);

            List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);
            int x = cellToCollapse.x;
            int y = cellToCollapse.y;

            // Check in NESW directions for a neighboring tile and filter possible tiles based on them.
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
                positionsTilesDict.Add(cellToCollapse, tileData);
                cellsToCollapse.Remove(cellToCollapse);

                // Update entropy for neighbors based on this new assignment
                UpdateNeighborEntropy(cellToCollapse, entropyDict);
            }
            else
            {
                Debug.LogWarning("No possible tile for tile at position: " + cellToCollapse);
            }
        }

        // Final painting of all tiles after the algorithm finishes.
        foreach (KeyValuePair<Vector2Int, TileData> kvp in positionsTilesDict)
        {

            Vector2Int position = kvp.Key;
            TileData tileData = kvp.Value;

            tilemapDrawer.PaintWaveCollapseTile(position, tileData.tileSprite);

            yield return delay;
        }
    }

    private Vector2Int GetCellWithLowestEntropy(List<Vector2Int> cellsToCollapse, Dictionary<Vector2Int, int> entropyDict)
    {
        // Sort the list of cells by their entropy and pick the first one (lowest entropy)
        return cellsToCollapse.OrderBy(c => entropyDict[c]).First();
    }

    private void UpdateNeighborEntropy(Vector2Int cell, Dictionary<Vector2Int, int> entropyDict)
    {
        // Logic to adjust entropy for neighboring cells based on the new assignment
        foreach (Vector2Int dir in Direction2D.cardinalDirectionsList)
        {
            Vector2Int neighbour = new Vector2Int(cell.x + dir.x, cell.y + dir.y);

            if (entropyDict.ContainsKey(neighbour))
            {
                // Recalculate possible tiles and adjust entropy
                int newEntropy = CalculateEntropyFor(neighbour);
                entropyDict[neighbour] = newEntropy;
            }
        }
    }

    private int CalculateEntropyFor(Vector2Int cell, Dictionary<Vector2Int, TileData> positionTilesDict)
    {
        // Get all possible tiles for this cell based on the current state of its neighbors.
        List<TileData> possibleTiles = new List<TileData>(allPossibleTiles);

        int x = cell.x;
        int y = cell.y;

        // Check NESW directions for neighbor constraints and filter possible tiles.
        for (int i = 0; i < Direction2D.cardinalDirectionsList.Count; i++)
        {
            Vector2Int currentDirection = Direction2D.cardinalDirectionsList[i];
            Vector2Int neighbour = new Vector2Int(x + currentDirection.x, y + currentDirection.y);

            if (positionTilesDict.TryGetValue(neighbour, out TileData neighbourTileData))
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

        // Return the count of possible tiles as the entropy.
        return possibleTiles.Count;
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

   
}
