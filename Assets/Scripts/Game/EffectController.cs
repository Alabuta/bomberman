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
            public Vector3 Position;
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
                    sprite.gameObject.transform.position = settings.Position + new Vector3(1.5f, 0, 0);
                    sprite.flipX = !settings.Flip.x;
                    sprite.size = new Vector2(s + 1, sprite.size.y);
                }
                else
                {
                    sprite.gameObject.transform.position = settings.Position + new Vector3(0.5f, 0, 0);
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
                    sprite.gameObject.transform.position = settings.Position + new Vector3(0, 1.5f, 0);
                    sprite.flipX = !settings.Flip.y;
                    sprite.size = new Vector2(sprite.size.x, s + 1);
                }
                else
                {
                    sprite.gameObject.transform.position = settings.Position + new Vector3(0, 0.5f, 0);
                    sprite.flipX = settings.Flip.y;
                    sprite.size = new Vector2(sprite.size.x, s);
                }
            }
        }

        private void OnValidate()
        {
            foreach (var settings in HorizontalSettings)
            {
                settings.Position = settings.Renderer.transform.position;

                settings.Flip.x = settings.Renderer.flipX;
                settings.Flip.y = settings.Renderer.flipY;
            }
        }
    }
}
