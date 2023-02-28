using Configs.Game;

namespace Game.Components.Events
{
    public readonly struct OnBombBlastActionComponent
    {
        public readonly PlayerTagConfig PlayerTag;

        public OnBombBlastActionComponent(PlayerTagConfig playerTag)
        {
            PlayerTag = playerTag;
        }
    }
}
