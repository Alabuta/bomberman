namespace Entity
{
    public class Player : IPlayer
    {
        public bool IsAlive { get; }

        public int Health { get; set; }
        public int MaxHealth { get; set; }

        public float Speed { get; set; }
        public float MaxSpeed { get; set; }
    }
}
