using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaveCollapse;

public class WaveCollapseGenerator : AbstractGenerator
{
    [Header("References")]
    [SerializeField] private GameObject tileCellObject = null;
    [SerializeField] private TilemapDrawer tilemapDrawer;
    [SerializeField] private TileData[] snowTiles;
    
    [Header("Generate settings")]
    [SerializeField] private Vector2Int gridDimensions = Vector2Int.one;
    [SerializeField] private bool delayedGeneration = false;

    private List<TileCell> gridCells = new List<TileCell>();
    private Coroutine delayedWaveCollapseCoroutine = null;

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

    }

    private IEnumerator WaveCollapseCoroutine()
    {
        yield return null;
    }

    private void CreateGrid()
    {
        Transform parent = new GameObject("TileCellParent").transform;

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                TileCell tileCell = Instantiate(tileCellObject, new Vector2(x, y), Quaternion.identity, parent).GetComponent<TileCell>();
                tileCell.CreateTileCell(snowTiles, false);
                gridCells.Add(tileCell);
            }
        }
    }

    public override void OnGenerate()
    {
        StartWaveCollapse();
    }

    public override void OnClear()
    {
        if (delayedWaveCollapseCoroutine != null)
        {
            StopAllCoroutines();
            delayedWaveCollapseCoroutine = null;
        }

        tilemapDrawer.Clear(tilemapDrawer.WfcTilemap);
    }
}
