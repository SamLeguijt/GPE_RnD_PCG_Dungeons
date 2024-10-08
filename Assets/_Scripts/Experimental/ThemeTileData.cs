using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ThemeTileData : ScriptableObject
{
    [field: SerializeField] public ThemesEnum Theme {  get; private set; } 
    
    [field: SerializeField] public TileBase FloorTile { get; private set; }
}
