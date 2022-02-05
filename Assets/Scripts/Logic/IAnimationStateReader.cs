using Entity;

namespace Logic
{
    public interface IAnimationStateReader
    {
        AnimatorState State { get; }

        void OnEnterState(AnimatorState stateHash);

        void OnStateExit(AnimatorState stateHash);
    }
}
