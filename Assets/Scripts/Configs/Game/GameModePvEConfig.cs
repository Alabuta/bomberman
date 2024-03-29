﻿using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(fileName = "GameModePvE", menuName = "Configs/Game/PvE Game Mode")]
    public sealed class GameModePvEConfig : GameModeConfig
    {
        public PlayerConfig PlayerConfig;
    }
}
