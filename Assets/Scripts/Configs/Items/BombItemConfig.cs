﻿using Configs.Effects;
using UnityEngine;

namespace Configs.Items
{
    [CreateAssetMenu(fileName = "BombItem", menuName = "Configs/Items/Bomb Item")]
    public class BombItemConfig : ItemConfig
    {
        public BlastEffectConfig BlastEffectConfig;
    }
}
