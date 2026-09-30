# Données du héros à l'initialisation d'un combat

> US1 — Combat récupère l'état initial du héros avant de démarrer un combat.

## Flux

`IHeroCombatDataProvider` (Application) est le point d'entrée unique utilisé
par les modules de combat. Il interroge en parallèle :

| Port Application | Propriétaire | Opération prévue par l'architecture |
|---|---|---|
| `IHeroSnapshotClient` | Player | `GetCombatantSnapshot` |
| `ICombatInventoryClient` | Rewards | `GetCombatInventorySnapshot` |

Il valide ensuite les données (PV/mana cohérents, stats et coûts non négatifs,
données du bon héros) et retourne un `HeroCombatData` immuable.

Toute exception empêche le démarrage du combat :

| Exception | HTTP | gRPC |
|---|---:|---|
| `HeroNotFoundException` | 404 | `NotFound` |
| `ExternalServiceUnavailableException` | 503 | `Unavailable` |
| `InvalidHeroCombatDataException` | 502 | `FailedPrecondition` |

Endpoint de consultation : `GET /api/v1/heroes/{heroId}/combat-data`.

## Mocks

Player et Rewards n'ont pas encore publié leurs contrats : les mocks
(`Infrastructure/ExternalServices/*/Mock*`) sont aujourd'hui la seule source.
Ils produisent les mêmes modèles que les futurs clients réels et passent par la
même validation. Tout résultat mocké porte `isMocked = true`.

Héros mockés : `11111111-1111-1111-1111-111111111111` (guerrier),
`22222222-2222-2222-2222-222222222222` (mage),
`33333333-3333-3333-3333-333333333333` (héros blessé). Tout autre identifiant
retourne « héros introuvable ».

## Brancher les vraies données

1. Référencer la version publiée de `Player.Contracts` (et `Rewards.Contracts`).
2. Écrire `PlayerHeroSnapshotGrpcClient : IHeroSnapshotClient` dans
   `Infrastructure/ExternalServices/Player` : deadline configurable, mapping
   manuel du contrat vers `HeroCombatSnapshot`, `NotFound` →
   `HeroNotFoundException`, `Unavailable`/`DeadlineExceeded` →
   `ExternalServiceUnavailableException`.
3. Dans `ExternalServicesRegistration`, remplacer l'enregistrement du mock par
   `AddHeroSnapshotClientWithMockFallback<PlayerHeroSnapshotGrpcClient>()`.

Le mock ne sert alors plus que de secours lorsque le service est **indisponible**.
Un « héros introuvable » renvoyé par Player n'est jamais remplacé par un mock.
Le secours se désactive avec `ExternalServices:Player:IsMockFallbackEnabled = false`
(idem `ExternalServices:Rewards`).

## État temporaire du héros (US2)

`IHeroFighterInitializer.InitializeAsync(heroId)` récupère les données
ci-dessus puis crée un `Fighter` (Domain) via `HeroFighterFactory` (mapping
manuel). Toute exception empêche le démarrage du combat.

Le `Fighter` est une copie locale au combat : il ne modifie jamais les données
de Player ou Rewards. Il porte :

- un `FighterId` propre au combat et l'`ExternalId` du héros chez Player ;
- un `InitialState` immuable, identique aux données reçues (PV, mana, stats,
  capacités, inventaire) ;
- un état courant modifiable : `TakeDamage`, `Heal`, `SpendMana`,
  `RestoreMana`, `ChangeStats`, `ConsumeItem` ;
- `IsFromMockedData` lorsque les données viennent des mocks.

Invariants : PV et mana restent entre 0 et leur maximum ; à 0 PV, le fighter
passe `KnockedOut` et toute opération est refusée
(`InvalidFighterOperationException`, HTTP 409 / gRPC `FailedPrecondition`).
Un fighter KO ne peut pas être soigné.

Le `Fighter` vit en mémoire : sa persistance viendra avec la création du
combat (`Fight`).
