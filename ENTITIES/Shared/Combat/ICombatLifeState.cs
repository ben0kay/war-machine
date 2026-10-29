// Optional actor-neutral lifetime gate for AI attacks; health remains owned by each receiver.
public interface ICombatLifeState
{
    bool IsDead { get; }
}
