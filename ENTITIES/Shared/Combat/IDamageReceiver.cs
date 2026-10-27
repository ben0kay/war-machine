// Shared target entry point. Detection sends a packet; target health resolves it and owns its reactions.
public interface IDamageReceiver
{
    void ReceiveHit(DamagePacket hit);
}
