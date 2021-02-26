using System;
using System.Collections.Generic;
using Core;

namespace App
{
    public class ApplicationHolder : Singleton<ApplicationHolder>
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public T Add<T>(T instance)
        {
            _instances.Add(typeof(T), instance);

            return instance;
        }

        public bool TryGet<T>(out object instance)
        {
            return _instances.TryGetValue(typeof(T), out instance);
        }

        protected override void DoRelease()
        {
            throw new NotImplementedException();
        }
    }
}
