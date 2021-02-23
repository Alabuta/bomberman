namespace Entity
{
    public interface IPlayer : IEntity
    {
        int BlastRadius { get; set; }

        int BombCapacity { get; set; }
    }
}
