using Game.Components;

namespace Level
{
    public interface ITileLoad
    {
        int LayerMask { get; }
        Component[] Components { get; }
    }
}
