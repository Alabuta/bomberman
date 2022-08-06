using Game.Components;
using Game.Components.Colliders;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine;

namespace Gizmos
{
    public class CollidersBoundsDrawer : MonoBehaviour, IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _filter;

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;
            UnityEngine.Gizmos.color = Color.green;

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                if (entity.Has<CircleColliderComponent>())
                {
                    ref var colliderComponent = ref entity.Get<CircleColliderComponent>();

                    UnityEngine.Gizmos.DrawWireSphere(fix2.ToXY(position), (float) colliderComponent.Radius);
                }
                else if (entity.Has<BoxColliderComponent>())
                {
                    ref var colliderComponent = ref entity.Get<BoxColliderComponent>();

                    var center = fix2.ToXY(colliderComponent.Offset + position);
                    var extents = fix2.ToXY(colliderComponent.Extent) * 2;

                    UnityEngine.Gizmos.DrawWireCube(center, extents);
                }
            }
        }

        public void Run()
        {
        }
    }
}
