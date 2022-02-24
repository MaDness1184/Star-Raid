using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemCountObsolete
{
    public Item item;
    [Range(0, 99)]
    public int count;
}

[CreateAssetMenu(fileName = " Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [TextArea]
    public string description;
    public Color tooltipColor = Color.red;
    public int countLimit = 99;
    public Sprite sprite;
    public GameObject prefab;
    public List<ItemCount> requiredMaterials;
    public List<ItemCount> resultComponents;

    public bool CheckItemRequirement(Item item)
    {
        bool found = false;
        foreach(ItemCount itemCount in requiredMaterials)
        {
            if (itemCount.item == item)
                found = true;
        }

        return found;
    }
}

public static class ItemSerializer
{
    public static void WriteItem(this NetworkWriter writer, Item item)
    {
        writer.WriteString(item.name);
    }

    public static Item ReadItem(this NetworkReader reader)
    {
        return Resources.Load(reader.ReadString()) as Item;
    }
}
