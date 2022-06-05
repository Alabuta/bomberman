using UnityEngine;

namespace Configs.Level.Tile
{
    [CreateAssetMenu(fileName = "SoftBlock", menuName = "Configs/Level/Soft Block")]
    public sealed class SoftBlockConfig : BlockConfig
    {
        public GameObject DestroyEffectPrefab;
    }
}
