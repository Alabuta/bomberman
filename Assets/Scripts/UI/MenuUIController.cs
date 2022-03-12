using App;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    [RequireComponent(typeof(PlayerInput))]
    public class MenuUIController : MonoBehaviour
    {
        [SerializeField]
        private MenuEntryController[] MenuEntryAnimators;

        private ISceneManager _sceneManager;

        private int _currentSelectedIndex;

        /*[Inject]
        public void Construct(ISceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }*/

        /*
        private void Awake()
        {
            Assert.IsNotNull(ApplicationHolder.Instance, "failed to initialize app holder");
            Assert.IsTrue(ApplicationHolder.Instance.TryGet(out _sceneManager), "failed to get scene manager");
        }
        */

        private void Start()
        {
            var menuStartEntry = MenuEntryAnimators[0];
            menuStartEntry.SetPointed(true);
        }

        [UsedImplicitly]
        public void OnSubmit(InputValue value)
        {
            var menuEntryAnimator = MenuEntryAnimators[_currentSelectedIndex];
            menuEntryAnimator.SubmitAndPlayAnimation(() => { }/*_sceneManager.StartNewGame()*/);
        }

        [UsedImplicitly]
        public void OnNavigate(InputValue value)
        {
            var direction = value.Get<Vector2>();

            _currentSelectedIndex = (int) (_currentSelectedIndex - direction.y) % MenuEntryAnimators.Length;

            if (_currentSelectedIndex < 0)
                _currentSelectedIndex = MenuEntryAnimators.Length + _currentSelectedIndex;

            for (var i = 0; i < MenuEntryAnimators.Length; i++)
            {
                var entry = MenuEntryAnimators[i];
                entry.SetPointed(i == _currentSelectedIndex);
            }
        }
    }
}
