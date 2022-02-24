using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemCountProbability
{
    public Item item;
    [Range(0,99)]
    public int minCount;
    [Range(0, 99)]
    public int maxCount;
    [Range(0,1)]
    public float probability;
}


[CreateAssetMenu(fileName = " LootTable", menuName = "Scriptable Objects/LootTable")]
public class LootTable : ScriptableObject
{
    public List<ItemCountProbability> lootTable;
}
