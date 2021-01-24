using System;

namespace Core
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T _instance;

        public static T Instance => _instance ??= new T();

        public static bool Try(Action<T> callback)
        {
            if (_instance == null)
                return false;

            callback(_instance);

            return true;
        }

        public static void Release()
        {
            if (_instance != null)
                _instance.DoRelease();

            _instance = null;
        }

        protected abstract void DoRelease();
    }
}
