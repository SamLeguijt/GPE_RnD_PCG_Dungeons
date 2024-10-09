using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class RoomGenerator : AbstractDungeonGenerator
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

    [Header("Generate settings")]
    [SerializeField] private Vector2Int dungeonSize = Vector2Int.one;
    [SerializeField] private Vector2Int roomSizeMin = Vector2Int.one;
    [SerializeField] private Vector2Int roomSizeMax = Vector2Int.one;
    [SerializeField, Range(0, 15)] private int roomPosOffset = 1;

    private HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
    private GameObject roomParentObject = null;

    [SerializeField] private TilemapDrawer tilemapDrawer = null;

    protected override void RunProceduralGeneration()
    {
        DestroyImmediate(roomParentObject);
        CurrentRooms.Clear();
        tilemapDrawer.Clear();
        GenerateDungeonFloor();
    }

    public void GenerateDungeonFloor()
    {
        HashSet<Vector2Int> roomsFloorGrid = new HashSet<Vector2Int>();

        var roomsBounds = GenerateRoomBounds();
        CurrentRooms = CreateRooms(roomsBounds);

        roomsFloorGrid = PopulateRoomBounds(roomsBounds);

        List<Vector2Int> roomCenters = new List<Vector2Int>();

        for (int i = 0; i < roomsBounds.Count; i++)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(roomsBounds[i].center));
        }

        // Might remove later.
        corridors = ConnectRooms(roomCenters);
        roomsFloorGrid.UnionWith(corridors);
    }

    private List<BoundsInt> GenerateRoomBounds()
    {
        var roomsBounds = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, new Vector3Int(dungeonSize.x, dungeonSize.y, 0)), roomSizeMin.x, roomSizeMin.y);
        roomsBounds = ApplyOffsetToRooms(roomsBounds, roomPosOffset);

        return roomsBounds;
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

            room.SetupRoom(roomBounds[i], tilemapDrawer,themeDataContainer);
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

    private void OnDrawGizmos()
    {
        // Dungeon bounds.
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(new Vector3(startPosition.x + (dungeonSize.x / 2), startPosition.y + (dungeonSize.y / 2), 0), new Vector3(dungeonSize.x, dungeonSize.y, 1));

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
