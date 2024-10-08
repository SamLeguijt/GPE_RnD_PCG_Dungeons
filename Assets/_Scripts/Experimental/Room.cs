using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.Rendering.DebugUI.Table;

public class Room : MonoBehaviour
{
    [field: SerializeField] public ThemesEnum RoomTheme { get; private set; }

    [SerializeField] private Vector2 roomMinSize = Vector2.zero;
    [SerializeField] private Vector2 roomMaxSize = Vector2.zero;

    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    public BoundsInt roomBounds;
    private ThemeTileData themeTileData;
    private TilemapDrawer tilemapDrawer;

    private static List<Color> tempThemeColors = new List<Color>
    {
        Color.cyan,
        Color.red,
        Color.green,
    };

    public void SetupRoom(BoundsInt bounds, TilemapDrawer drawer, ThemeDataContainer themeDataContainer, ThemesEnum theme = ThemesEnum.None)
    {
        roomBounds = bounds;
        transform.position = roomBounds.center;
        RoomTheme = GetTheme(theme);
        themeTileData = themeDataContainer.GetThemeTileData(RoomTheme);

        DrawRoomTiles(drawer);
    }

    public void DrawRoomTiles(TilemapDrawer drawer)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        for (int y = 0; y < roomBounds.size.y; y++)
        {
            for (int x = 0; x < roomBounds.size.x; x++)
            {
                Vector2Int position = (Vector2Int)roomBounds.min + new Vector2Int(x, y);
                floor.Add(position);
            }
        }

        drawer.PaintTiles(floor, themeTileData.FloorTile);

        float randomNumber = Random.Range(0f, 1f);
        if (randomNumber < 0.5f)
        {
            var puddle = CreatePuddle();
            drawer.PaintTiles(puddle, themeTileData.PuddleTile);
        }
    }

    private HashSet<Vector2Int> CreatePuddle()
    {
        SimpleRandomWalkSO puddleParameters = themeTileData.PuddleParameters;

        HashSet<Vector2Int> randomPath = ProceduralGenerationAlgorithms.SimpleRandomWalk(
            new Vector2Int((int)roomBounds.center.x, (int)roomBounds.center.y), puddleParameters.walkLength);
        return randomPath;
    }

    private ThemesEnum GetTheme(ThemesEnum desiredTheme = ThemesEnum.None)
    {
        ThemesEnum theme = ThemesEnum.None;

        if (desiredTheme != ThemesEnum.None)
            theme = desiredTheme;
        else
        {
            int randomSelection = Random.Range(1, RoomGenerator.RoomThemes.Count + 1);
            theme = (ThemesEnum)randomSelection;
        }

        return theme;
    }

    public void OnDrawGizmos()
    {
        if (roomBounds.size != Vector3Int.zero)
        {
            Gizmos.color = tempThemeColors[(int)RoomTheme - 1];

            Vector3 roomCenter = roomBounds.center;
            Vector3 roomSize = roomBounds.size;

            Gizmos.DrawWireCube(roomCenter, roomSize);
        }
    }
}
