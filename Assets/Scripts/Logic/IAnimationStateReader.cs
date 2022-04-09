using Game;

namespace Logic
{
    public interface IAnimationStateReader
    {
        AnimatorState State { get; }

        void OnEnterState(AnimatorState state);

        void OnExitState(AnimatorState state);
    }
}
