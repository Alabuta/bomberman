using App;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UI
{
    [RequireComponent(typeof(PlayerInput))]
    public class MenuUIController : MonoBehaviour
    {
        private ISceneManager _sceneManager;

        private GameObject _currentSelected;

        /*[Inject]
        public void Construct(ISceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }*/

        private void Awake()
        {
            Assert.IsNotNull(ApplicationHolder.Instance, "failed to initialize app holder");
            Assert.IsTrue(ApplicationHolder.Instance.TryGet(out _sceneManager), "failed to get scene manager");
        }

        private void Start()
        {
            _currentSelected = EventSystem.current.firstSelectedGameObject;
        }

        [UsedImplicitly]
        public void OnSubmit(InputValue value)
        {
            // Debug.LogWarning("OnSubmit");
            _sceneManager.StartNewGame();
            // _animationFinished = () => _sceneManager.StartNewGame();
        }

        [UsedImplicitly]
        public void OnNavigate(InputValue value)
        {
            /*var direction = value.Get<Vector2>();

            Debug.LogWarning($"OnNavigate {direction}");*/
        }
    }
}
