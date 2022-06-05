using JetBrains.Annotations;
using UnityEngine;
using Component = Game.Components.Component;

namespace Level
{
    public interface ITileLoad
    {
        int LayerMask { get; }
        Component[] Components { get; }

        [CanBeNull]
        GameObject DestroyEffectPrefab { get; }

        bool TryGetComponent<T>(out T component) where T : Component;
    }
}
