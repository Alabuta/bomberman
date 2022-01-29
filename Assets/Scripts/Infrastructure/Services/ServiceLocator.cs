namespace Infrastructure.Services
{
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        public static ServiceLocator Container => _instance ??= new ServiceLocator();

        public void RegisterSingle<TService>(TService implementation) where TService : IService
        {
            Implementation<TService>.Instance = implementation;
        }

        public TService Single<TService>() where TService : IService
        {
            return Implementation<TService>.Instance;
        }

        private static class Implementation<TService> where TService : IService
        {
            public static TService Instance;
        }
    }
}
