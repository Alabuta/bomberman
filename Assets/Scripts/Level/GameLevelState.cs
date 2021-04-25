using System;
using Entity;
using UnityEngine;

namespace Level
{
    public interface IGameLevelState
    {
        event Action<Item> ItemEffectAppliedEvent;
    }

    public class GameLevelState : IGameLevelState
    {
        /*
         * In-Game current state data
         * hero HP, alive mobs number, etc...
         */

        public event Action<Item> ItemEffectAppliedEvent;

        private readonly GameLevelGridModel _gameLevelGridModel;

        public GameLevelState(GameLevelGridModel gameLevelGridModel)
        {
            _gameLevelGridModel = gameLevelGridModel;

            ItemEffectAppliedEvent = OnItemEffectApplied;
        }

        private void OnItemEffectApplied(Item item)
        {
            Debug.LogWarning(item);
        }
    }
}
