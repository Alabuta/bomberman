using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntityController
    {
        fix Speed { get; set; }
        float PlaybackSpeed { get; }
        int2 Direction { get; set; }

        fix2 WorldPosition { get; set; }

        void Kill();
    }
}
