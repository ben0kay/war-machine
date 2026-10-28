// AM0: per-firearm magazine and reserve state. No Nodes, polling, inventory coupling or animations.
using Godot;

public sealed class FirearmAmmo
{
    private readonly int _capacity;
    private readonly bool _unlimitedReserve;
    private int _magazine, _reserve;
    public int MagazineRounds => _magazine;
    public int MagazineCapacity => _capacity;
    public int ReserveRounds => _reserve;
    public bool HasUnlimitedReserveAmmo => _unlimitedReserve;
    public bool IsReloading { get; private set; }
    public bool CanShoot => !IsReloading && _magazine > 0;
    public bool IsEmpty => _magazine == 0;
    public bool CanReload => !IsReloading && _magazine < _capacity && (_unlimitedReserve || _reserve > 0);

    // =========================================================
    // The gun scene owns this instance; equip/unequip never creates a fresh magazine.
    public FirearmAmmo(FirearmSettings settings)
    {
        _capacity = Mathf.Clamp(settings.MagazineCapacity, 1, 1000);
        _magazine = _capacity;
        _unlimitedReserve = settings.DebugUnlimitedReserveAmmo;
        _reserve = Mathf.Clamp(settings.InitialReserveRounds, 0, 1000000);
    }

    // =========================================================
    // Deduct only after the shared controller has accepted and executed a valid shot.
    public bool TryConsumeShot()
    {
        if (!CanShoot) return false;
        --_magazine;
        return true;
    }

    // =========================================================
    // Lock fire immediately; only the gun's one-shot timer may finish this action.
    public bool TryBeginReload()
    {
        if (!CanReload) return false;
        IsReloading = true;
        return true;
    }

    // =========================================================
    // Top off without discarding chambered/remaining rounds; finite reserve spends only what fits.
    public int FinishReload()
    {
        if (!IsReloading) return 0;
        IsReloading = false;
        int missing = _capacity - _magazine;
        int loaded = _unlimitedReserve ? missing : Mathf.Min(missing, _reserve);
        _magazine += loaded;
        if (!_unlimitedReserve) _reserve -= loaded;
        return loaded;
    }

    // =========================================================
    // Swap/unequip interrupts reload without adding ammunition or clearing existing rounds.
    public bool CancelReload()
    {
        if (!IsReloading) return false;
        IsReloading = false;
        return true;
    }
}
