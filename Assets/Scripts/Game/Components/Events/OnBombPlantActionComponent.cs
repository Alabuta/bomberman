using Configs.Entity;
using Configs.Game;
using Math.FixedPointMath;

namespace Game.Components.Events
{
    public readonly struct OnBombPlantActionComponent
    {
        public readonly PlayerTagConfig PlayerTag;

        public readonly fix2 Position;

        public readonly BombConfig BombConfig;
        public readonly fix BlastDelay;
        public readonly fix BombBlastDamage;
        public readonly int BombBlastRadius;

        public OnBombPlantActionComponent(
            PlayerTagConfig playerTag,
            fix2 position,
            BombConfig bombConfig,
            fix blastDelay,
            fix bombBlastDamage,
            int bombBlastRadius)
        {
            PlayerTag = playerTag;

            Position = position;

            BombConfig = bombConfig;
            BlastDelay = blastDelay;
            BombBlastDamage = bombBlastDamage;
            BombBlastRadius = bombBlastRadius;
        }
    }
}
