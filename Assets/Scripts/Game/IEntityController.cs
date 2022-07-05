using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game
{
    public interface IEntityController
    {
        fix Speed { set; }
        int2 Direction { set; }

        fix2 WorldPosition { set; }

        float PlaybackSpeed { get; }

        void Die();

        void TakeDamage(int damage);
    }
}
