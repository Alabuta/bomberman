namespace Entity
{
    public interface IEntity
    {
        bool IsAlive { get; }

        int Health { get; set; }
        int MaxHealth { get; set; }

        float Speed { get; set; }
        float MaxSpeed { get; set; }
    }
}
