using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
    public class BlastEffectController : MonoBehaviour
    {
        [Serializable]
        private class EffectSettings
        {
            public SpriteRenderer Renderer;

            [SerializeField, HideInInspector]
            public bool2 Flip;

            [SerializeField, HideInInspector]
            public Vector3 PositionOffset;

            [SerializeField, HideInInspector]
            public Vector2[] OffsetVectors;
        }

        [SerializeField]
        private EffectSettings[] Settings;

        public void Construct(int blastRadius, IEnumerable<int> sizes)
        {
            foreach (var (settings, size) in Settings.Zip(sizes, (settings, size) => (settings, size)))
            {
                var isBlastRadiusRestricted = size != 0 && size != blastRadius;

                var sprite = settings.Renderer;

                sprite.flipX = isBlastRadiusRestricted ? !settings.Flip.x : settings.Flip.x;
                sprite.flipY = isBlastRadiusRestricted ? !settings.Flip.y : settings.Flip.y;

                sprite.size = sprite.size * settings.OffsetVectors[1] + size * settings.OffsetVectors[0];

                var spriteTransform = sprite.gameObject.transform;
                spriteTransform.position = transform.position + settings.PositionOffset;

                if (!isBlastRadiusRestricted)
                    continue;

                sprite.size += settings.OffsetVectors[0];
                spriteTransform.position += settings.PositionOffset.normalized * size;
            }
        }

        private void OnValidate()
        {
            foreach (var settings in Settings)
            {
                settings.PositionOffset = settings.Renderer.transform.position - transform.position;

                settings.Flip.x = settings.Renderer.flipX;
                settings.Flip.y = settings.Renderer.flipY;

                settings.OffsetVectors = new[]
                {
                    new Vector2(Mathf.Abs(settings.PositionOffset.normalized.x),
                        Mathf.Abs(settings.PositionOffset.normalized.y)),
                    new Vector2(Mathf.Abs(settings.PositionOffset.normalized.y),
                        Mathf.Abs(settings.PositionOffset.normalized.x))
                };
            }
        }
    }
}
