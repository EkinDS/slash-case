using System.Collections.Generic;

public sealed class ResourcesLevelCatalog : ILevelCatalog
{
    private readonly string resourcePath;

    public ResourcesLevelCatalog(string resourcePath)
    {
        this.resourcePath = resourcePath;
    }

    public List<PixelFlowLevelData> LoadAll()
    {
        return PixelFlowLevelLoader.LoadAllFromResources(resourcePath);
    }
}
