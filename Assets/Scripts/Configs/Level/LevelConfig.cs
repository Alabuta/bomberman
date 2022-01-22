using Configs.Level.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level/Level Config")]
    public sealed class LevelConfig : ConfigBase
    {
        [Header("General Parameters")]
        public int Index;
        public string Name;

        public int OriginalPixelsPerUnits = 16;

        public int4 ViewportPadding = int4.zero;

        [Header("General Prefabs")]
        public GameObject Walls;

        [Header("Block Parameters")]
        public HardBlock HardBlock;
        public SoftBlock SoftBlock;
        public PortalBlock PortalBlock;

        [Space(16)]
        public LevelStageConfig[] LevelStages;
    }
}
