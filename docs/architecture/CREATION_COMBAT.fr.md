# Création d'un combat (US11)

> US11 — Combat crée un combat entre le héros et les monstres générés, afin de
> démarrer l'affrontement.

## Déclenchement

Dungeon détecte une rencontre et demande la création du combat. Dungeon ne
fournit pas de monstre concret : il fournit uniquement la session et le type de
rencontre. Le service Combat reste propriétaire du choix et de la génération des
monstres.

L'appel est un gRPC interne `CombatService/CreateCombat` :

| Champ | Rôle |
|---|---|
| `session_id` | Session de jeu à laquelle le combat appartient. Un combat appartient à exactement une session. |
| `encounter_id` | Rencontre à l'origine du combat. Une rencontre ne crée jamais deux combats. |
| `command_id` | Identifiant de la commande. La même commande ne crée jamais deux combats. |
| `encounter_type` | `ENEMY`, `SUB_BOSS` ou `BOSS`. |

Réponse : `combat_id` (GUID unique), les identifiants de la session et de la
rencontre, le statut du combat et l'état de combat initial de chaque
participant.

Le contrat vit dans `Combat.Contracts/Protos/combat_combat_v1.proto` et suit
les règles de [ADR-GLOB-007](../adr/ADR-GLOB-007-protobuf-contract-packages.md).

## Types de rencontre

`EncounterType` (Domain) → classe de monstre :

| `EncounterType` | `MonsterClass` |
|---|---|
| `Enemy` | `Ordinary` |
| `SubBoss` | `SubBoss` |
| `Boss` | `Boss` |

La classe décrit une catégorie, pas un monstre. Le monstre concret est une
`MonsterDefinition` (Goblin, Dragon, Necromancer…) choisie par Combat dans le
catalogue. Player et Dungeon ne décident jamais du monstre.

## Déroulé

1. **Idempotence** : le combat est cherché par `command_id`, puis par
   `encounter_id`. Un combat déjà existant est renvoyé tel quel. Un `command_id`
   réutilisé avec d'autres paramètres lève `IdempotencyKeyReuseException`.
2. **Roster** : `ISessionHeroesClient` (Player) fournit les héros de la
   session. Player reste la source de vérité de la composition du groupe.
3. **Contexte de rencontre** : `IEncounterContextClient` (Dungeon) fournit la
   quantité de monstres et leur niveau.
4. **Participants** : chaque héros est initialisé via `IHeroFighterInitializer`
   (US2) à partir des données Player et Rewards (US1). Un seul échec annule
   toute la création : le combat ne persiste pas.
5. **Monstres** : une `MonsterDefinition` est tirée dans la classe correspondant
   à la rencontre, autant de fois que la rencontre en exige. Les statistiques
   suivent la règle de palier et de taille de groupe.
6. **Persistance** : le combat et l'état de chaque participant sont enregistrés
   dans la commande. Le comportement de transaction Mediator porte le commit.

## Règles métier

- Un combat possède au moins un héros et au moins un monstre.
- Le `combat_id` est un GUID généré par Combat.
- Un participant apparaît une seule fois dans un combat.
- L'état initial d'un participant est immuable ; l'état courant évolue pendant
  le combat sans jamais être renvoyé à Player ou Rewards.
- PV et mana restent compris entre 0 et leur maximum. À 0 PV, le participant
  passe `KnockedOut` et ne peut plus agir.
- Un `command_id` ou un `encounter_id` déjà connu renvoie le combat existant
  au lieu d'en créer un second.

## Modèle de données

| Table | Contenu |
|---|---|
| `CombatInstances` | `CombatId`, `CombatSessionId`, `CombatEncounterId`, `CombatCommandId`, `CombatEncounterType`, `CombatStatus`, `CombatStartedAt` |
| `CombatFighters` | Identité du participant, état courant (PV, mana, statistiques, état), état initial, capacités, objets et effets |

`CombatCommandId` et `CombatEncounterId` sont uniques en base : la règle
d'idempotence est garantie par la base, pas seulement par le code.

## Erreurs

| Exception | HTTP | gRPC |
|---|---:|---|
| `SessionNotFoundException` | 404 | `NotFound` |
| `NoCombatParticipantsException` | 409 | `FailedPrecondition` |
| `InvalidCombatEncounterException` | 422 | `FailedPrecondition` |
| `IdempotencyKeyReuseException` | 409 | `AlreadyExists` |

## Clients factices

Player et Dungeon n'ont pas publié leurs contrats. Les mocks
(`Infrastructure/ExternalServices/Mock*`) servent de source unique et produisent
les mêmes modèles que les futurs clients réels.

- `MockSessionHeroesClient` renvoie le roster du catalogue héros simulé.
- `MockEncounterContextClient` renvoie la quantité et le niveau de chaque type
  de rencontre, configurés dans `ExternalServices:Dungeon`.

## Brancher les vraies données

1. Référencer les versions publiées de `Player.Contracts` et `Dungeon.Contracts`.
2. Écrire `PlayerSessionHeroesGrpcClient : ISessionHeroesClient` et
   `DungeonEncounterContextGrpcClient : IEncounterContextClient` dans
   `Infrastructure/ExternalServices`.
3. Dans `ExternalServicesRegistration`, remplacer l'enregistrement du mock par
   `AddSessionHeroesClientWithMockFallback<TRealClient>()` ou
   `AddEncounterContextClientWithMockFallback<TRealClient>()`.

Le secours ne sert alors plus que lorsque le service est **indisponible** : un
« héros introuvable » ou une session inconnue renvoyés par le service réel ne
sont jamais remplacés par un mock.

## Tester la création d'un combat

### Avec la stack Docker

```bash
docker compose up -d --build
```

L'API écoute sur `http://localhost:8080`, le gRPC interne sur
`http://localhost:8081`, PostgreSQL sur `5433` et RabbitMQ sur `15672`.

```bash
# Un combat BOSS
grpcurl -plaintext \
  -import-path Combat.Contracts/Protos \
  -proto combat_combat_v1.proto \
  -d "{\"session_id\":\"$(uuidgen)\",\"encounter_id\":\"$(uuidgen)\",\"command_id\":\"$(uuidgen)\",\"encounter_type\":\"ENCOUNTER_TYPE_BOSS\"}" \
  localhost:8081 combat.v1.CombatService/CreateCombat
```

Types acceptés : `ENCOUNTER_TYPE_ENEMY`, `ENCOUNTER_TYPE_SUB_BOSS`,
`ENCOUNTER_TYPE_BOSS`.

La réponse contient le `combatId`, le statut et l'état de combat initial de
chaque participant. Rejouer la même commande renvoie le même `combatId` ;
réutiliser un `commandId` avec d'autres paramètres renvoie `AlreadyExists`.

```bash
# Vérifier la persistance
docker compose exec -T postgres psql -U combat -d combat \
  -c 'SELECT "CombatId", "CombatEncounterType", "CombatStatus" FROM "CombatInstances";'
```

### Sans Docker

```bash
dotnet run --project Combat.Presentation/Combat.Presentation.csproj
```

L'application applique les migrations et les données de démonstration au
démarrage en `Development`. Elle a besoin d'un PostgreSQL reachable, configuré
dans `ConnectionStrings:DefaultConnection`.

### Tests automatisés

```bash
# Les tests d'intégration créent leur base et la suppriment à la fin.
COMBAT_TEST_DATABASE_CONNECTION="Host=localhost;Port=5433;Database=combat;Username=combat" \
  dotnet test --solution Combat.Presentation.slnx
```

Les tests couvrent le domaine, le cas d'usage, la persistance et le point
d'entrée gRPC.
