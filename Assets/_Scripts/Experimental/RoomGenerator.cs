using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{

    public static List<Room> ActiveRooms = new List<Room>();

    [SerializeField] private Vector3Int dungeonSize = Vector3Int.one;
    [SerializeField] private float roomMinWidth = 1f;
    [SerializeField] private float roomMinHeight = 1f;


    private void Start()
    {
       // ActiveRooms.Clear();
       // ActiveRooms = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt(Vector3Int.zero, dungeonSize), (int)roomMinWidth, (int)roomMinHeight, true);

        Debug.Log("Rooms populated: " + ActiveRooms.Count);
    }

    public static List<Room> CreateRooms(List<BoundsInt> roomBounds)
    {
        List<Room> rooms = new List<Room>();

        for (int i = 0; i < roomBounds.Count; i++)
        {
            Room room = new Room(roomBounds[i]);
            rooms.Add(room);
        }

        ActiveRooms = rooms;
        return rooms; 
    }

    private void OnDrawGizmos()
    {
        if (ActiveRooms.Count < 0)
            return;

        foreach (Room room in ActiveRooms)
        {
            // Check if roomBounds has been initialized
            if (room.roomBounds.size != Vector3Int.zero)
            {
                // Set the color for the Gizmos (optional)
                Gizmos.color = Color.green;

                // Calculate the center of the room bounds
                Vector3 roomCenter = room.roomBounds.center;
                Vector3 roomSize = room.roomBounds.size;

                // Draw a wireframe cube representing the room's bounds
                Gizmos.DrawWireCube(roomCenter, roomSize);
            }
        }
    }
}
