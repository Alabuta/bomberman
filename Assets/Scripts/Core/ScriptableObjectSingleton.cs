using UnityEngine;

namespace Core
{
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<T>($"Singletons/{typeof(T).Name}");

                return _instance;
            }
        }
    }
}
