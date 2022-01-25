using Configs.Level.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "Level", menuName = "Configs/Level/Level")]
    public sealed class LevelConfig : ConfigBase
    {
        [Header("General Parameters")]
        public int Index;
        public string Name;

        public string SceneName;

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
