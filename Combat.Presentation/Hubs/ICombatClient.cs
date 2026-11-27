namespace Combat.Presentation.Hubs;

/// <summary>Events the Combat Hub pushes to the group <c>combat:{CombatId}</c>.</summary>
public interface ICombatClient
{
    /// <summary>The full, authoritative state after every accepted change.</summary>
    Task CombatStateChanged(CombatSnapshotDto snapshot);
}
