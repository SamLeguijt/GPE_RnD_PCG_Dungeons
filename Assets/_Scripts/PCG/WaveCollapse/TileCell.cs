using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveCollapse
{
    public enum TileCellType
    {
        Center,
        CornerNW,
        SideN,
        CornerNE,
        SideE,
        CornerSE,
        SideS,
        CornerSW,
        SideW,
        Puddle,
    }

    public class TileCell : MonoBehaviour
    {
        public bool IsCollapsed { get; set; } = false;
        
        [SerializeField] public ThemesEnum tileTheme;
        [SerializeField] public TileCellType tileType; 
        [SerializeField] public TileData[] tileOptions;

        [SerializeField] public TilemapDrawer tilemapDrawer;
        [SerializeField] public Vector2Int Position;
        
        public void Create(TileData[] possibleTiles, bool isCollapsed, ThemesEnum theme)
        {
            tileTheme = theme;
            tileOptions = possibleTiles;
            IsCollapsed = isCollapsed;
        }

        public void Recreate(TileData[] tiles) 
        {
            tileOptions = tiles;
        }

        public TileCell GetNeighbour(Vector2Int direction)
        {
            return this;
        }

        public int GetEntropy()
        {
            return int.MaxValue;
        }

        public void Collapse(TileData tileVisuals)
        {
            //tileData = tileVisuals;
            //tilemapDrawer.PaintWaveCollapseTile(Position, tileData.tileSprite);
            //IsCollapsed = true;
        }
    }
}
