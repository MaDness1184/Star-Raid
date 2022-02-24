using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootDropController : MonoBehaviour
{
    [SerializeField] private GameObject _sceneItemReplica;
    [SerializeField] private LootTable _lootTable; 

    public GameObject sceneItemReplica { get { return _sceneItemReplica; } }
    public LootTable lootTable { get { return _lootTable; } }

    public List<ItemCountObsolete> GenerateLoot()
    {
        List<ItemCountObsolete> result = new List<ItemCountObsolete>();

        foreach(ItemCountProbability rngGod in _lootTable.lootTable)
        {
            if (Random.value > rngGod.probability) continue;

            ItemCountObsolete itemCount = new ItemCountObsolete();
            itemCount.item = rngGod.item;
            itemCount.count = Random.Range(rngGod.minCount, rngGod.maxCount);

            result.Add(itemCount);
        }

        return result;
    }
}
