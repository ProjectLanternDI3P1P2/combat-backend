using Combat.Application.Models;
using Combat.Application.Ports;
using System.Collections.Concurrent;

namespace Combat.Infrastructure.HeroMocks;

public sealed class MockHeroCombatStateSource : IHeroCombatStateSource
{
    private static readonly Guid HeroId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StrikeAbilityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GuardAbilityId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SwordItemId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PotionItemId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly ConcurrentDictionary<string, MockSession> _sessions = new(StringComparer.Ordinal);

    public Task<HeroCombatState> StartSessionAsync(
        string sessionId,
        string scenario,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<HeroCombatState> states = CreateStates(scenario);
        var session = new MockSession(states);
        _sessions[sessionId] = session;

        return Task.FromResult(session.Current);
    }

    public Task<HeroCombatState> GetCurrentAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetSession(sessionId).Current);
    }

    public Task<HeroCombatState> AdvanceAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetSession(sessionId).Advance());
    }

    public void EndSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    private MockSession GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out MockSession? session)
            ? session
            : throw new KeyNotFoundException($"No hero mock session exists for connection '{sessionId}'.");
    }

    private static IReadOnlyList<HeroCombatState> CreateStates(string scenario)
    {
        return scenario.ToLowerInvariant() switch
        {
            HeroMockScenarios.Complete => [CreateComplete(HeroMockScenarios.Complete, 1, 84, 31)],
            HeroMockScenarios.Zero => [CreateZero()],
            HeroMockScenarios.Empty => [CreateEmpty()],
            HeroMockScenarios.Partial => [CreatePartial()],
            HeroMockScenarios.Changing =>
            [
                CreateComplete(HeroMockScenarios.Changing, 1, 84, 31),
                CreateComplete(HeroMockScenarios.Changing, 2, 57, 18)
            ],
            _ => throw new ArgumentException(
                $"Unknown hero mock scenario '{scenario}'. Expected one of: {string.Join(", ", HeroMockScenarios.All)}.",
                nameof(scenario))
        };
    }

    private static HeroCombatState CreateComplete(string scenario, long sequence, int currentHealth, int currentMana)
    {
        return new HeroCombatState(
            sequence,
            scenario,
            HeroCombatStateAvailability.Ready,
            new HeroCombatant(
                HeroId,
                "Ariane",
                currentHealth,
                120,
                currentMana,
                45,
                8,
                19,
                14,
                17,
                "ready",
                [
                    new HeroAbility(StrikeAbilityId, "Arc Strike", 8, "enemy", "available"),
                    new HeroAbility(GuardAbilityId, "Guard", 0, "self", null)
                ],
                [new HeroEquipmentItem(SwordItemId, "Bronze Sword", "mainHand")],
                [new HeroConsumable(PotionItemId, "Health Potion", 2)]));
    }

    private static HeroCombatState CreateZero()
    {
        return new HeroCombatState(
            1,
            HeroMockScenarios.Zero,
            HeroCombatStateAvailability.Ready,
            new HeroCombatant(
                HeroId,
                "Zero",
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                "provided-zero",
                [],
                [],
                [new HeroConsumable(PotionItemId, "Empty Flask", 0)]));
    }

    private static HeroCombatState CreateEmpty()
    {
        return new HeroCombatState(
            1,
            HeroMockScenarios.Empty,
            HeroCombatStateAvailability.Ready,
            new HeroCombatant(
                HeroId,
                "Unburdened",
                50,
                50,
                10,
                10,
                1,
                5,
                5,
                5,
                "ready",
                [],
                [],
                []));
    }

    private static HeroCombatState CreatePartial()
    {
        return new HeroCombatState(
            1,
            HeroMockScenarios.Partial,
            HeroCombatStateAvailability.Ready,
            new HeroCombatant(
                HeroId,
                null,
                38,
                null,
                null,
                20,
                3,
                null,
                7,
                null,
                null,
                [new HeroAbility(StrikeAbilityId, "Unknown technique", null, null, null)],
                null,
                [new HeroConsumable(PotionItemId, "Uncounted Potion", null)]));
    }

    private sealed class MockSession(IReadOnlyList<HeroCombatState> states)
    {
        private readonly object _sync = new();
        private int _index;

        public HeroCombatState Current
        {
            get
            {
                lock (_sync)
                {
                    return states[_index];
                }
            }
        }

        public HeroCombatState Advance()
        {
            lock (_sync)
            {
                if (_index < states.Count - 1)
                {
                    _index++;
                }

                return states[_index];
            }
        }
    }
}
