using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "Match3/BoardConfig")]
public class BoardConfig : ScriptableObject
{
    [Header("Grid Size")]
    [Min(1)] public int columns = 8;
    [Min(1)] public int rows = 8;

    [Header("Tile Prefab")]
    public GameObject tilePrefab;            // Assign your basic Tile prefab here

    [Header("Tile Variants")]
    public List<Sprite> tileSprites;         // The different sprites (colors) to use
}
