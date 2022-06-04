using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
    public class EffectController : MonoBehaviour
    {
        [Serializable]
        private class Settings
        {
            public SpriteRenderer Renderer;

            [SerializeField, HideInInspector]
            public bool2 Flip;

            [SerializeField, HideInInspector]
            public Vector3 PositionOffset;
        }

        [SerializeField]
        private Settings[] HorizontalSettings;

        [SerializeField]
        private Settings[] VerticalSettings;

        public void SetSize(int blastRadius, int4 sizes)
        {
            var masks = new[]
            {
                new Vector2(1, 0),
                new Vector2(0, 1)
            };

            foreach (var (settings, index) in HorizontalSettings.Select((s, i) => (s, i)))
            {
                var sprite = settings.Renderer;

                var size = sizes[index];
                var isSizeRestricted = size != 0 && size != blastRadius;

                sprite.flipX = isSizeRestricted ? !settings.Flip.x : settings.Flip.x;
                sprite.size = sprite.size * masks[1] + new Vector2(size, size) * masks[0];

                var spriteTransform = sprite.gameObject.transform;
                spriteTransform.position = transform.position + settings.PositionOffset;

                if (!isSizeRestricted)
                    continue;

                sprite.size += masks[0];
                spriteTransform.position += settings.PositionOffset.normalized;
            }

            foreach (var (settings, index) in VerticalSettings.Select((s, i) => (s, i)))
            {
                var sprite = settings.Renderer;

                var size = sizes[index + 2];
                var isSizeRestricted = size != 0 && size != blastRadius;

                sprite.flipY = isSizeRestricted ? !settings.Flip.y : settings.Flip.y;
                sprite.size = sprite.size * masks[0] + new Vector2(size, size) * masks[1];

                var spriteTransform = sprite.gameObject.transform;
                spriteTransform.position = transform.position + settings.PositionOffset;

                if (!isSizeRestricted)
                    continue;

                sprite.size += masks[1];
                spriteTransform.position += settings.PositionOffset.normalized;
            }
        }

        private void OnValidate()
        {
            foreach (var settings in HorizontalSettings)
            {
                settings.PositionOffset = settings.Renderer.transform.position - transform.position;

                settings.Flip.x = settings.Renderer.flipX;
                settings.Flip.y = settings.Renderer.flipY;
            }

            foreach (var settings in VerticalSettings)
            {
                settings.PositionOffset = settings.Renderer.transform.position - transform.position;

                settings.Flip.x = settings.Renderer.flipX;
                settings.Flip.y = settings.Renderer.flipY;
            }
        }
    }
}
