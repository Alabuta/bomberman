using System.Threading.Tasks;

namespace Infrastructure.States
{
    public interface IExitableState
    {
        void Exit();
    }

    public interface IGameState : IExitableState
    {
        Task Enter();
    }

    public interface IPayloadedState<in TPayload> : IExitableState
    {
        Task Enter(TPayload levelStage);
    }
}
