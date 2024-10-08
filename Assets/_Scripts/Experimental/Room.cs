using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Room : MonoBehaviour
{
    [field: SerializeField] public ThemesEnum RoomTheme { get; private set; }

    [SerializeField] private Vector2 roomMinSize = Vector2.zero;
    [SerializeField] private Vector2 roomMaxSize = Vector2.zero;

    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    public BoundsInt roomBounds;

    private static List<Color> tempThemeColors = new List<Color>
    {
        Color.cyan,
        Color.red,
        Color.green,
    };

    public void SetupRoom(BoundsInt bounds, ThemesEnum theme = ThemesEnum.None)
    {
        roomBounds = bounds;
        transform.position = roomBounds.center;
        RoomTheme = GetTheme(theme);
    }

    public void DrawRoomTiles(TilemapVisualizer tilemapVisualizer)
    {
        this.tilemapVisualizer = tilemapVisualizer;
    }

    private ThemesEnum GetTheme(ThemesEnum desiredTheme = ThemesEnum.None)
    {
        ThemesEnum theme = ThemesEnum.None;

        if (desiredTheme != ThemesEnum.None)
            theme = desiredTheme;
        else
        {
            int randomSelection = Random.Range(1, RoomGenerator.RoomThemes.Count +1);
            theme = (ThemesEnum)randomSelection;
        }

        return theme;
    }

    public void OnDrawGizmos()
    {
        if (roomBounds.size != Vector3Int.zero)
        {
            Gizmos.color = tempThemeColors[(int)RoomTheme -1];

            Vector3 roomCenter = roomBounds.center;
            Vector3 roomSize = roomBounds.size;

            Gizmos.DrawWireCube(roomCenter, roomSize);
        }
    }
}
