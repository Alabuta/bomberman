using System;
using App;
using UnityEngine;
using UnityEngine.Assertions;

namespace UI
{
    public enum UIEventTypes
    {
        Nothing,
        NewGamePressed,
        BattlePressed,
        SetupPressed,
        PasswordPressed
    }

    public class StartMenuUIEventsForwarder : MonoBehaviour
    {
        private ISceneManager _sceneManager;

        public event Action<UIEventTypes> NewGamePressedEvent;

        private Action AnimationFinished;

        private void Awake()
        {
            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");

            Assert.IsTrue(applicationHolder.TryGet(out _sceneManager), "failed to get scene manager");
        }

        public void MenuEntryAnimationFinished()
        {
            AnimationFinished?.Invoke();
        }

        public void NewGamePressed()
        {
            AnimationFinished = () => _sceneManager.StartNewGame();
        }

        public void BattlePressed() => NewGamePressedEvent?.Invoke(UIEventTypes.BattlePressed);

        public void SetupPressed() => NewGamePressedEvent?.Invoke(UIEventTypes.SetupPressed);

        public void PasswordPressed() => NewGamePressedEvent?.Invoke(UIEventTypes.PasswordPressed);
    }
}
