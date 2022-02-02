using Data;
using Infrastructure.Services;

namespace Infrastructure.States.SaveLoad
{
    public interface ISaveLoadService : IService
    {
        void SaveProgress();

        PlayerProgress LoadProgress();
    }
}
