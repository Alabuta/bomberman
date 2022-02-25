/*using Infrastructure.Services.Input;
using Zenject;

namespace App
{
    public class BootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindInputService();
        }

        private void BindInputService()
        {
            Container
                .Bind<IInputService>()
                .To<InputService>()
                // .FromInstance(Infrastructure.Game.InputService)
                .AsSingle();
            /*var inputService = Container
                .Bind<IInputService>()
                .To<InputService>()
                .FromComponentInNewPrefab(inputServicePrefab)
                .AsSingle();#1#
        }
    }
}*/


