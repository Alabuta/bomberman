using Configs.Game;
using Game.Components;

namespace Level
{
    public interface ITileLoad
    {
        GameTagConfig GameTag { get; }
        Component[] Components { get; }
    }
}
