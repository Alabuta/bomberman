namespace Items
{
    /*public abstract class Item<TConfig> : IItem where TConfig : ItemConfig
    {
        public ItemConfig ItemConfig { get; }

        public ItemController Controller { get; set; }

        public int LayerMask { get; }

        public Component[] Components { get; protected set; }
        public GameObject DestroyEffectPrefab => null;

        protected Item(TConfig itemConfig, ItemController controller)
        {
            ItemConfig = itemConfig;
            Controller = controller;

            LayerMask = itemConfig.LayerMask;

            ColliderComponent2 collider = itemConfig.Collider switch
            {
                BoxColliderComponentConfig config => new BoxColliderComponent2(config),
                CircleColliderComponentConfig config => new CircleColliderComponent2(config),
                _ => null
            };

            Components = new Component[]
            {
                collider
            };
        }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = Components.OfType<T>().FirstOrDefault();
            return component != default;
        }
    }*/
}
