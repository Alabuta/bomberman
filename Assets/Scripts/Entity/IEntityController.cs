using Unity.Mathematics;

namespace Entity
{
    public interface IEntityController
    {
        float Speed { get; set; }
        float PlaybackSpeed { get; }
        float2 Direction { get; set; }

        float3 WorldPosition { get; }

        void Kill();
    }
}
