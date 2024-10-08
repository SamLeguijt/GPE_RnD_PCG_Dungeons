using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Room
{
    [field: SerializeField] public ThemesEnum RoomTheme { get; private set; }

    [SerializeField] private Vector2 roomMinSize = Vector2.zero;
    [SerializeField] private Vector2 roomMaxSize = Vector2.zero;

    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    public BoundsInt roomBounds;

    public void GenerateRoom(Vector3Int position, Vector2 sizeMin, Vector2 sizeMax)
    {
        this.roomMinSize = sizeMin;
        this.roomMaxSize = sizeMax;

        Vector3Int randomSize = new Vector3Int(Mathf.RoundToInt(Random.Range(sizeMin.x, sizeMax.x)), Mathf.RoundToInt(Random.Range(sizeMin.y, sizeMax.y)));

        roomBounds = new BoundsInt(position, randomSize);

        Debug.Log("Generated");
    
    }

    public Room(BoundsInt bounds)
    {
        roomBounds = bounds;
    }

    public void PaintRoom(TilemapVisualizer tilemapVisualizer)
    {
        this.tilemapVisualizer = tilemapVisualizer;
    }

    public void OnDrawGizmos()
    {
        // Check if roomBounds has been initialized
        if (roomBounds.size != Vector3Int.zero)
        {
            // Set the color for the Gizmos (optional)
            Gizmos.color = Color.green;

            // Calculate the center of the room bounds
            Vector3 roomCenter = roomBounds.center;
            Vector3 roomSize = roomBounds.size;

            // Draw a wireframe cube representing the room's bounds
            Gizmos.DrawWireCube(roomCenter, roomSize);
        }
    }

    
}
