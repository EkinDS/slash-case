using UnityEngine;

public sealed class ResourcesPigPrefabProvider : IPigPrefabProvider
{
    private const string PigPrefabResourcePath = "Pig/Prefabs/Pig";

    private GameObject pigPrefab;

    public GameObject GetPigPrefab()
    {
        if (pigPrefab == null)
        {
            pigPrefab = Resources.Load<GameObject>(PigPrefabResourcePath);
        }

        return pigPrefab;
    }
}
