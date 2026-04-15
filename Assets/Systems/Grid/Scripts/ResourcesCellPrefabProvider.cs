using UnityEngine;

public sealed class ResourcesCellPrefabProvider : ICellPrefabProvider
{
    private const string CellPrefabResourcePath = "Grid/Prefabs/Cell";

    private GameObject cellPrefab;

    public GameObject GetCellPrefab()
    {
        if (cellPrefab == null)
        {
            cellPrefab = Resources.Load<GameObject>(CellPrefabResourcePath);
        }

        return cellPrefab;
    }
}
