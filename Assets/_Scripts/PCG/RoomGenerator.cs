using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class RoomGenerator : AbstractGenerator
{
    public static List<Room> CurrentRooms = new List<Room>();
    public static List<ThemesEnum> RoomThemes = new List<ThemesEnum>
    {
        ThemesEnum.Snow,
        ThemesEnum.Fire,
        ThemesEnum.Grass
    };

    [Header("References")]
    [SerializeField] private ThemeDataContainer themeDataContainer = null;
    [SerializeField] private TilemapDrawer tilemapDrawer = null;
    [SerializeField] private SimpleRandomWalkSO randomWalkParameters = null;

    [Header("Generate settings")]
    [SerializeField] private Vector2Int dungeonSize = Vector2Int.one;
    [SerializeField] private Vector2Int roomSizeMin = Vector2Int.one;
    [SerializeField] private Vector2Int roomSizeMax = Vector2Int.one;
    [SerializeField, Range(0, 15)] private int roomPosOffset = 1;
    [SerializeField] private bool useAdditionalRandomWalk = false;

    [field: Header("Visualization")]
    [field: SerializeField] public bool DrawGizmos { get; private set; } = false;

    private HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
    private static GameObject roomParentObject = null;

    public override void OnGenerate()
    {
        Clear();
        GenerateDungeonFloor();
    }

    public override void OnClear()
    {
        DestroyImmediate(roomParentObject);
        CurrentRooms.Clear();
        corridors.Clear();
        tilemapDrawer.Clear();
    }

    public void GenerateDungeonFloor()
    {
        HashSet<Vector2Int> roomsFloorGrid = new HashSet<Vector2Int>();

        var roomsBounds = GenerateRoomBounds();
        CurrentRooms = CreateRooms(roomsBounds);

        List<Vector2Int> roomCenters = new List<Vector2Int>();

        for (int i = 0; i < roomsBounds.Count; i++)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(roomsBounds[i].center));

            if (useAdditionalRandomWalk)
            {
                int extraWalkFromCornersAmount = Random.Range(0, 5);
                var randomWalkedFloor = RandomWalkFromRoomCorners(roomsBounds[i], extraWalkFromCornersAmount, randomWalkParameters);

                CurrentRooms[i].AddPositionsToRoom(randomWalkedFloor);
            }
        }

        corridors = ConnectRooms(roomCenters);
        tilemapDrawer.PaintCorridorTiles(corridors, themeDataContainer.GetThemeTileData(ThemesEnum.None).FloorTile);
    }

    private List<BoundsInt> GenerateRoomBounds()
    {
        var roomsBounds = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)StartPosition, new Vector3Int(dungeonSize.x, dungeonSize.y, 0)), roomSizeMin.x, roomSizeMin.y);
        roomsBounds = ApplyOffsetToRooms(roomsBounds, roomPosOffset);

        return roomsBounds;
    }

    private HashSet<Vector2Int> RandomWalkFromRoomCorners(BoundsInt room, int cornersToWalkFrom, SimpleRandomWalkSO parameters)
    {
        System.Random random = new System.Random();
        HashSet<Vector2Int> randomFloor = new HashSet<Vector2Int>();

        Mathf.Clamp(cornersToWalkFrom, 0, 4);

        float quarterWidthX1 = room.center.x - (room.center.x - room.min.x) / 2;
        float quarterWidthX2 = room.center.x + (room.max.x - room.center.x) / 2;
        float quarterHeightY1 = room.center.y - (room.center.y - room.min.y) / 2;
        float quarterHeightY2 = room.center.y + (room.max.y - room.center.y) / 2;

        Vector2Int bottomLeftQuarter = new Vector2Int((int)quarterWidthX1, (int)quarterHeightY1);
        Vector2Int bottomRightQuarter = new Vector2Int((int)quarterWidthX2, (int)quarterHeightY1);
        Vector2Int topLeftQuarter = new Vector2Int((int)quarterWidthX1, (int)quarterHeightY2);
        Vector2Int topRightQuarter = new Vector2Int((int)quarterWidthX2, (int)quarterHeightY2);

        List<Vector2Int> cornerStartPoints = new List<Vector2Int>()
        {
            bottomLeftQuarter,
            bottomRightQuarter,
            topLeftQuarter,
            topRightQuarter
        };

        for (int i = cornerStartPoints.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            Vector2Int temp = cornerStartPoints[i];
            cornerStartPoints[i] = cornerStartPoints[randomIndex];
            cornerStartPoints[randomIndex] = temp;
        }

        for (int i = 0; i < cornersToWalkFrom; i++)
        {
            var floor = RunRandomWalk(parameters, cornerStartPoints[i]);
            randomFloor.UnionWith(floor);
        }

        return randomFloor;
    }


    public List<Room> CreateRooms(List<BoundsInt> roomBounds)
    {
        List<Room> rooms = new List<Room>();

        roomParentObject = new GameObject("Rooms");
        roomParentObject.transform.SetParent(this.transform);

        for (int i = 0; i < roomBounds.Count; i++)
        {
            GameObject roomObject = new GameObject($"Room_{i}", typeof(Room));
            roomObject.transform.SetParent(roomParentObject.transform);
            Room room = roomObject.GetComponent<Room>();

            room.SetupRoom(this, roomBounds[i], tilemapDrawer, themeDataContainer);
            rooms.Add(room);
        }

        return rooms;
    }

    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomCenters)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestRoomTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }

        return corridors;
    }

    private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);

        while (position.y != destination.y)
        {
            if (destination.y > position.y)
            {
                position += Vector2Int.up;
            }
            else if (destination.y < position.y)
            {
                position += Vector2Int.down;
            }
            corridor.Add(position);
        }
        while (position.x != destination.x)
        {
            if (destination.x > position.x)
            {
                position += Vector2Int.right;
            }
            else if (destination.x < position.x)
            {
                position += Vector2Int.left;
            }
            corridor.Add(position);
        }
        return corridor;
    }

    private Vector2Int FindClosestRoomTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;
        foreach (var position in roomCenters)
        {
            float currentDistance = Vector2.Distance(position, currentRoomCenter);
            if (currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }
        return closest;
    }

    private HashSet<Vector2Int> PopulateRoomBounds(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomsList)
        {
            for (int col = 0; col < room.size.x; col++)
            {
                for (int row = 0; row < room.size.y; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    private List<BoundsInt> ApplyOffsetToRooms(List<BoundsInt> rooms, int offset)
    {
        List<BoundsInt> offsetRooms = new List<BoundsInt>();

        foreach (var room in rooms)
        {
            Vector3Int newMin = room.min + new Vector3Int(offset, offset, 0);
            Vector3Int newSize = new Vector3Int(
                room.size.x - 2 * offset,
                room.size.y - 2 * offset,
                room.size.z
            );

            BoundsInt adjustedRoom = new BoundsInt(newMin, newSize);
            offsetRooms.Add(adjustedRoom);
        }

        return offsetRooms;
    }

    private void OnDrawGizmosSelected()
    {
        if (!DrawGizmos)
            return;

        // Dungeon bounds.
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(new Vector3(StartPosition.x + (dungeonSize.x / 2), StartPosition.y + (dungeonSize.y / 2), 0), new Vector3(dungeonSize.x, dungeonSize.y, 1));

        // Corridors visualization
        if (corridors != null && corridors.Count > 0)
        {
            Gizmos.color = Color.black;

            foreach (Vector2Int corridorPosition in corridors)
            {
                Vector3 corridorTilePosition = new Vector3(corridorPosition.x + 0.5f, corridorPosition.y + 0.5f, 0);
                Gizmos.DrawWireCube(corridorTilePosition, Vector3.one);
            }
        }
    }
}
