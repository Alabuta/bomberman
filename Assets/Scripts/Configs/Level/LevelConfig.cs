﻿using System.Collections.Generic;
using Configs.Enemy;
using Configs.Level.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level/Level Config", order = 1)]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("General Configs")]
        public string Name;

        public int ColumnsNumber;
        public int RowsNumber;

        [Range(0, 100)]
        public int SoftBlocksCoverage = 30;

        public float3 CameraPosition = float3.zero;

        public int2[] PlayersSpawnCorners = {int2.zero};

        [Header("General Prefabs")]
        public GameObject Walls;

        [Header("Enemy Configs")]
        public EnemyConfig[] Enemies;
        public EnemyConfig[] PortalEnemies;

        [Header("Block Configs")]
        public PortalBlock PortalBlock;

        public HardBlock HardBlock;
        public SoftBlock SoftBlock;

        /*
         * [Header("PowerUp Configs")]
         * public PowerUpConfig[] PowerUps;
         *
         * [Header("Pit and Hazard Configs")]
         * ...
         */
    }
}
