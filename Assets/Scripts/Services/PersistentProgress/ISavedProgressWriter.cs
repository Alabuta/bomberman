using Data;

namespace Services.PersistentProgress
{
    public interface ISavedProgressReader
    {
        void LoadProgress(PlayerProgress progress);
    }

    public interface ISavedProgressWriter : ISavedProgressReader
    {
        void UpdateProgress(PlayerProgress progress);
    }
}
