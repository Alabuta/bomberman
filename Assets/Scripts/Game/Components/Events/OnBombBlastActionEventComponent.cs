using Configs.Game;

namespace Game.Components.Events
{
    public readonly struct OnBombBlastActionEventComponent
    {
        public readonly PlayerTagConfig PlayerTag;

        public OnBombBlastActionEventComponent(PlayerTagConfig playerTag)
        {
            PlayerTag = playerTag;
        }
    }
}
