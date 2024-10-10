using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AbstractGenerator : MonoBehaviour
{
    [field: Header("Abstract settings")]
    [field:SerializeField] public Vector2Int StartPosition {  get; protected set; }

    public void Generate()
    {
        OnGenerate();
    }

    public void Clear()
    {
        OnClear();
    }

    public abstract void OnGenerate();
    
    public abstract void OnClear();


    protected HashSet<Vector2Int> RunRandomWalk(SimpleRandomWalkSO parameters, Vector2Int position)
    {
        var currentPosition = position;
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        for (int i = 0; i < parameters.iterations; i++)
        {
            var path = ProceduralGenerationAlgorithms.SimpleRandomWalk(currentPosition, parameters.walkLength);
            floorPositions.UnionWith(path);
            if (parameters.startRandomlyEachIteration)
                currentPosition = floorPositions.ElementAt(Random.Range(0, floorPositions.Count));
        }
        return floorPositions;
    }
}
