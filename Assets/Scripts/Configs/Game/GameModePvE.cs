﻿using Configs.Entity;
using Configs.Level;
using UnityEngine;

namespace Configs.Game
{
    [CreateAssetMenu(fileName = "GameModePvE", menuName = "Configs/Game/PvE Mode")]
    public sealed class GameModePvE : ScriptableObject
    {
        public LevelConfig[] Levels;

        public PlayerConfig[] Players;

        public BombermanConfig BombermanConfig;
    }
}
