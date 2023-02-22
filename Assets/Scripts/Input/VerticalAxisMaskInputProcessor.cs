using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class VerticalAxisMaskInputProcessor : InputProcessor<Vector2>
    {
#if UNITY_EDITOR
        static VerticalAxisMaskInputProcessor() =>
            Initialize();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() =>
            InputSystem.RegisterProcessor<VerticalAxisMaskInputProcessor>();

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (value.y != 0)
                value.x = 0;

            return value;
        }
    }
}
