using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Room : MonoBehaviour
{
    public List<Vector2Int> RoomPositions { get; private set; }
    [field: SerializeField] public ThemesEnum RoomTheme { get; private set; }
    public Color ThemeColor { get; private set; }

    [SerializeField] private Vector2 roomMinSize = Vector2.zero;
    [SerializeField] private Vector2 roomMaxSize = Vector2.zero;

    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    public BoundsInt roomBounds;

    private ThemeTileData themeTileData;
    private TilemapDrawer tilemapDrawer;
    private RoomGenerator roomGenerator;

    public void SetupRoom(RoomGenerator generator, BoundsInt bounds, TilemapDrawer drawer, ThemeDataContainer themeDataContainer, ThemesEnum theme = ThemesEnum.None)
    {
        roomGenerator = generator;
        roomBounds = bounds;
        transform.position = roomBounds.center;
        RoomTheme = GetTheme(theme);
        RoomPositions = GetRoomPositions(bounds);
        themeTileData = themeDataContainer.GetThemeTileData(RoomTheme);
        tilemapDrawer = drawer;
        ThemeColor = ThemeDataContainer.GetThemeColor(RoomTheme);

        DrawRoomTiles(GetRoomPositions(bounds), drawer);
    }

    public void AddPositionsToRoom(HashSet<Vector2Int> positions)
    {
        HashSet<Vector2Int> roomPositionsSet = RoomPositions.ToHashSet();
        roomPositionsSet.UnionWith(positions);
        RoomPositions = roomPositionsSet.ToList();

        List<Vector2Int> newPositions = positions.ToList();
        int newMinX = roomBounds.xMin;
        int newMaxX = roomBounds.xMax;
        int newMinY = roomBounds.yMin;
        int newMaxY = roomBounds.yMax;

        for (int i = 0; i < newPositions.Count; i++)
        {
            if (newPositions[i].x < newMinX)
                newMinX = newPositions[i].x;

            if (newPositions[i].x > newMaxX)
                newMaxX = newPositions[i].x;

            if (newPositions[i].y < newMinY)
                newMinY = newPositions[i].y;

            if (newPositions[i].y > newMaxY)
                newMaxY = newPositions[i].y;
        }
        
        newMaxX = newMaxX != roomBounds.xMax ? newMaxX +1 : roomBounds.xMax;
        newMaxY = newMaxY != roomBounds.yMax ? newMaxY +1 : roomBounds.yMax;    

        BoundsInt bounds = new BoundsInt
        {
            min = new Vector3Int(newMinX, newMinY),
            max = new Vector3Int(newMaxX, newMaxY)
        };

        roomBounds = bounds;
        DrawRoomTiles(positions, tilemapDrawer);
    }

    private List<Vector2Int> GetRoomPositions(BoundsInt bounds)
    {
        HashSet<Vector2Int> roomFloor = new HashSet<Vector2Int>();

        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                Vector2Int position = (Vector2Int)bounds.min + new Vector2Int(x, y);
                roomFloor.Add(position);
            }
        }

        return roomFloor.ToList();
    }


    public void DrawRoomTiles(IEnumerable<Vector2Int> positions, TilemapDrawer drawer)
    {
        drawer.PaintRoomTiles(positions, themeTileData.FloorTile);

        float randomNumber = Random.Range(0f, 1f);
        if (randomNumber < 0.5f)
        {
            var puddle = CreatePuddle();
            drawer.PaintRoomTiles(puddle, themeTileData.PuddleTile);
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
        if (roomGenerator != null)
        {
            if (!roomGenerator.drawGizmos)
                return;
        }

        // Draw bounds.
        if (roomBounds.size != Vector3Int.zero)
        {
            Gizmos.color = ThemeColor;

            Vector3 roomCenter = roomBounds.center;
            Vector3 roomSize = roomBounds.size;

            Gizmos.DrawWireCube(roomCenter, roomSize);
        }

        // Draw each room cell.
        if (RoomPositions != null && RoomPositions.Count > 0)
        {
            for (int i = 0; i < RoomPositions.Count; i++)
            {
                Vector3 position = new Vector3(RoomPositions[i].x + 0.5f, RoomPositions[i].y + 0.5f, 0);

                Gizmos.DrawWireCube(position, new Vector3(1, 1, 1));
            }
        }
    }
}
