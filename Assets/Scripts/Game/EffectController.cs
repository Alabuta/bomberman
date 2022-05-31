using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
    public class EffectController : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer[] HorizontalEnds;

        [SerializeField]
        private SpriteRenderer[] VerticalEnds;

        public void SetSize(int4 size)
        {
            foreach (var (sprite, index) in HorizontalEnds.Select((go, i) => (go, i)))
                sprite.size = new Vector2(size[index], sprite.size.y);

            foreach (var (sprite, index) in VerticalEnds.Select((go, i) => (go, i)))
                sprite.size = new Vector2(sprite.size.x, size[index + 2]);
        }
    }
}
