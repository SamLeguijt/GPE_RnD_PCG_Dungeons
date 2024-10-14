using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WaveCollapse
{
    [CreateAssetMenu(menuName = "PCG/WaveCollapse/Tile", fileName = "Tile_")]
    public class TileData : ScriptableObject
    {
        public TileBase tileSprite;
        public ThemesEnum theme;
        public TileCellType tileType;
        [Space]
        public List<TileData> northNeighbours = new List<TileData>();
        public List<TileData> eastNeighbours = new List<TileData>();
        public List<TileData> southNeighbours = new List<TileData>();
        public List<TileData> westNeighbours = new List<TileData>();
    }
}

