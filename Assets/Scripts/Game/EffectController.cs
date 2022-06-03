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

            [SerializeField, HideInInspector]
            public int2 PositionOffsetVector;
        }

        [SerializeField]
        private Settings[] HorizontalSettings;

        [SerializeField]
        private Settings[] VerticalSettings;

        public void SetSize(int blastRadius, int4 size)
        {
            foreach (var (settings, index) in HorizontalSettings.Select((s, i) => (s, i)))
            {
                var sprite = settings.Renderer;
                var s = size[index];
                if (s != 0 && s != blastRadius)
                {
                    sprite.gameObject.transform.position = transform.position + settings.PositionOffset +
                                                           settings.PositionOffset.normalized;
                    sprite.flipX = !settings.Flip.x;
                    sprite.size = new Vector2(s + 1, sprite.size.y);
                }
                else
                {
                    sprite.gameObject.transform.position = transform.position + settings.PositionOffset;
                    sprite.flipX = settings.Flip.x;
                    sprite.size = new Vector2(s, sprite.size.y);
                }
            }

            foreach (var (settings, index) in VerticalSettings.Select((go, i) => (go, i)))
            {
                var sprite = settings.Renderer;
                var s = size[index + 2];
                if (s != 0 && s != blastRadius)
                {
                    sprite.gameObject.transform.position = transform.position + settings.PositionOffset +
                                                           settings.PositionOffset.normalized;
                    sprite.flipY = !settings.Flip.y;
                    sprite.size = new Vector2(sprite.size.x, s + 1);
                }
                else
                {
                    sprite.gameObject.transform.position = transform.position + settings.PositionOffset;
                    sprite.flipY = settings.Flip.y;
                    sprite.size = new Vector2(sprite.size.x, s);
                }
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
        }
    }
}
