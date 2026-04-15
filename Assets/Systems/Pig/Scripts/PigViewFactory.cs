using UnityEngine;

public sealed class PigViewFactory : IPigViewFactory
{
    private readonly IPigPrefabProvider pigPrefabProvider;

    public PigViewFactory(IPigPrefabProvider pigPrefabProvider)
    {
        this.pigPrefabProvider = pigPrefabProvider;
    }

    public IPigView Create(Transform parent, string viewName, PixelPigColor color, int ammo)
    {
        var pigPrefab = pigPrefabProvider != null ? pigPrefabProvider.GetPigPrefab() : null;

        if (pigPrefab == null)
        {
            Debug.LogError("Pig prefab provider returned null.");
            return null;
        }

        var pigObject = Object.Instantiate(pigPrefab, parent, false);
        pigObject.name = viewName;

        var pigView = pigObject.GetComponent<PigView>();

        if (pigView == null)
        {
            Debug.LogError("Pig prefab is missing PigView.");
            Object.Destroy(pigObject);
            return null;
        }

        pigView.Initialize(parent, color);
        pigView.SetAmmo(ammo);
        pigView.ClearClickAction();
        return pigView;
    }
}
