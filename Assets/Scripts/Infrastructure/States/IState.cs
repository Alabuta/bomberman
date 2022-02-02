namespace Infrastructure.States
{
    public interface IExitableState
    {
        void Exit();
    }

    public interface IGameState : IExitableState
    {
        void Enter();
    }

    public interface IPayloadedState<in TPayload> : IExitableState
    {
        void Enter(TPayload levelStage);
    }
}
