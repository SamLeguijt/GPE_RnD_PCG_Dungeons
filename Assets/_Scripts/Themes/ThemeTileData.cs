using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileData = WaveCollapse.TileData;

[CreateAssetMenu(menuName = "ScriptableObjects/New Theme tile data", fileName = "ThemeTileData_")]
public class ThemeTileData : ScriptableObject
{
    [field: SerializeField] public ThemesEnum Theme { get; private set; }
    [field: SerializeField] public SimpleRandomWalkSO PuddleParameters { get; private set; }
    [field: SerializeField] public TileBase FloorTile { get; private set; }
    [field: SerializeField] public TileBase PuddleTile { get; private set; }
    [field: SerializeField] public TileBase ObstacleTile { get; private set; }
    [field: SerializeField] public TileBase DecorationTile { get; private set; }

    [field: SerializeField] public List<TileData> PossibleTiles { get; private set; }
}
