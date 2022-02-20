using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntityController
    {
        float Speed { get; set; }
        float PlaybackSpeed { get; }
        float2 Direction { get; set; }

        fix2 WorldPosition { get; set; }

        void Kill();
    }
}
