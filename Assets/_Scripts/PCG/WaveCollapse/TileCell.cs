using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveCollapse
{
    public class TileCell : MonoBehaviour
    {
        public bool IsCollapsed {  get; private set; }
        
        [SerializeField] private TileData tileData;
        [SerializeField] private ThemesEnum tileTheme;
        [SerializeField] private TileData[] currentNeighbours;

        [SerializeField] private TilemapDrawer tilemapDrawer;
        [SerializeField] private Vector2Int Position;
        
        public void Create(TileData[] possibleTiles, bool isCollapsed, ThemesEnum theme)
        {
            tileTheme = theme;
            currentNeighbours = possibleTiles;
            IsCollapsed = isCollapsed;
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
            tileData = tileVisuals;
            tilemapDrawer.PaintWaveCollapseTile(Position, tileData.tileSprite);
        }
    }
}
