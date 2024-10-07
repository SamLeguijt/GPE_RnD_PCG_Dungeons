﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    public static BoundsInt FirstGeneratedRoom => firstGeneratedRoom;

    [SerializeField]
    private int minRoomWidth = 4, minRoomHeight = 4;
    [SerializeField]
    private int dungeonWidth = 20, dungeonHeight = 20;
    [SerializeField]
    [Range(0, 10)]
    private int offset = 1;
    [SerializeField]
    private bool useRandomWalkRooms = false;
    private static BoundsInt firstGeneratedRoom;

    protected override void RunProceduralGeneration()
    {
        CreateRooms();
    }

    private void CreateRooms()
    {
        var roomsList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        if (useRandomWalkRooms)
        {
            floor = CreateRandomFloor(roomsList);
        }
        else
        {
            floor = CreateSimpleRooms(roomsList);
        }

        List<Vector2Int> roomCenters = new List<Vector2Int>();

        for (int i = 0; i < roomsList.Count; i++)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(roomsList[i].center));

            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomsList[i].center.x), Mathf.RoundToInt(roomsList[i].center.y));
            int extraWalkFromCornersAmount = Random.Range(0, 5);
            var randomWalkedFloor = RandomWalkFromRoomCorners(roomsList[i], extraWalkFromCornersAmount, randomWalkParameters);


            floor.UnionWith(randomWalkedFloor);
        }

        firstGeneratedRoom = roomsList[0];

        HashSet<Vector2Int> corridors = ConnectRooms(roomCenters);
        floor.UnionWith(corridors);

        tilemapVisualizer.PaintFloorTiles(floor);
        WallGenerator.CreateWalls(floor, tilemapVisualizer);
    }

    private HashSet<Vector2Int> CreateRandomFloor(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        for (int i = 0; i < roomsList.Count; i++)
        {
            var roomBounds = roomsList[i];
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBounds.center.x), Mathf.RoundToInt(roomBounds.center.y));
            var roomFloor = RunRandomWalk(randomWalkParameters, roomCenter);

            foreach (var position in roomFloor)
            {
                if (position.x >= (roomBounds.xMin + offset) && position.x <= (roomBounds.xMax - offset) && position.y >= (roomBounds.yMin - offset) && position.y <= (roomBounds.yMax - offset))
                {
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomCenters)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private HashSet<Vector2Int> RandomWalkFromRoomCorners(BoundsInt room, int cornersToWalkFrom, SimpleRandomWalkSO parameters)
    {
        System.Random random = new System.Random();
        HashSet<Vector2Int> randomFloor = new HashSet<Vector2Int>();

        Mathf.Clamp(cornersToWalkFrom, 0, 4);

        // Calculate quarter points relative to the center of the room
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

    private Vector2Int FindClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
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

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomsList)
        {
            for (int col = offset; col < room.size.x - offset; col++)
            {
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }
}
