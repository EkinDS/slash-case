public interface ILevelSaveLoad
{
    void Save(PixelFlowLevelData levelData);
    void Save(int levelId, PixelFlowLevelData levelData);
    PixelFlowLevelData Load();
    PixelFlowLevelData Load(int levelId);
    void Clear();
    void Clear(int levelId);
}
