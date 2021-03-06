﻿using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "BombConfig", menuName = "Configs/Entity/Bomb Config")]
    public sealed class BombConfig : ScriptableObject
    {
        [Header("General Parameters")]
        public string Name;

        public GameObject Prefab;

        public int LifetimeSec = 3;
    }
}
