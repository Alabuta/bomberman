using Game.Components;

namespace Level
{
    public interface ITileLoad
    {
        int LayerMask { get; }
        Component[] Components { get; }

        bool TryGetComponent<T>(out T component) where T : Component;
    }
}
