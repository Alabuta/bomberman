using Entity;

namespace Logic
{
    public interface IAnimationStateReader
    {
        AnimatorState State { get; }

        void OnEnterState(AnimatorState state);

        void OnStateExit(AnimatorState state);
    }
}
