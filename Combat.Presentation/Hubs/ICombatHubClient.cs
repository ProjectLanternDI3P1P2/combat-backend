using Combat.Presentation.DTO;

namespace Combat.Presentation.Hubs;

public interface ICombatHubClient
{
    Task CombatStateChanged(HeroCombatStateDto state);
}
