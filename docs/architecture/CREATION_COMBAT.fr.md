# Création d'un combat (US11)

> US11 — Combat crée un combat entre le héros et les monstres générés, afin de
> démarrer l'affrontement.

## Déclenchement

Dungeon détecte un contact entre le héros et un pack de 1 à N mobs (ou le boss)
et demande la création du combat par le gRPC interne `CombatService/CreateCombat`
(`Combat.Contracts/Protos/combat_combat_v1.proto`, champs ajoutés de façon
additive, ADR-GLOB-007) :

| Champ | Rôle |
|---|---|
| `session_id`, `encounter_id`, `command_id` | Session, rencontre (= clé d'idempotence côté Dungeon), commande. Une rencontre ne crée jamais deux combats. |
| `encounter_type` | `ENEMY` (collision) ou `BOSS`. |
| `run_id`, `floor` (1-basé), `room_id` | Où a eu lieu la rencontre. |
| `participants[]` | Héros du groupe (`hero_id`) et, s'ils sont connus, les PV/mana qu'il leur reste dans la run (`current_hp`, `current_mana`). |
| `monsters[]` | Un élément par mob engagé : `element_id` (id du mob Dungeon) et `is_boss`. |

Les mobs Dungeon (`zombie`, `bat`, `goblin`, `mage`) ne décrivent **qu'un
déplacement**. Combat choisit le monstre : une `MonsterDefinition` de la classe
`Ordinary` (ou `Boss`), tirée de façon **déterministe** à partir de
`element_id`. Le même mob devient donc toujours le même monstre, même après une
fuite.

Réponse : `combat_id`, `run_id`, le statut (`active` / `finished`) et l'état de
combat initial de chaque participant (avec `class_code`, `sprite_key`,
`is_boss`).

## Déroulé

1. **Idempotence** : le combat est cherché par `command_id`, puis par
   `encounter_id`. Un combat existant est renvoyé tel quel ; même clé avec un
   autre pack → `IdempotencyKeyReuseException` (`AlreadyExists`).
2. **Héros** : chaque participant est initialisé avec les données **réelles** de
   Player (`GetCombatantSnapshot` : stats, compétences, apparence). Les PV/mana
   envoyés par Dungeon remplacent ceux de Player. Player indisponible →
   `Unavailable` : aucun combat, Dungeon réessaiera avec la même clé. Aucun mock
   n'est servi au runtime.
3. **Monstres** : statistiques de la définition, ajustées par étage et taille
   du groupe (`MonsterStatsCalculator`).
4. **Démarrage** : le moteur ouvre le premier tour (les monstres plus rapides
   jouent d'abord), avec une échéance serveur pour le tour du héros.
5. **Persistance** : combat, participants, tours, journal d'événements et
   commandes traitées dans la même transaction.

## Déroulement du combat (CombatHub)

Le gameplay passe par SignalR `/hubs/combats` (ADR-GLOB-010) :
`JoinCombat { combatId }`, `SubmitAction { commandId, combatId, actorId, type,
targetId?, abilityCode? }` (`attack`, `ability`, `defend`, `flee`, `pass`),
`PassTurn`, événement `CombatStateChanged`. Chaque appel répond un
`CombatCommandResult` (`accepted`, `error`, `snapshot`, `events` de la commande).
Le moteur reprend les règles de la boucle de combat : initiative par vitesse,
une action par tour, tours des monstres joués par le serveur, esquive,
critique (10 %, x1,5), défense (-50 %), fuite (60 %, interdite contre un boss).
Le timer est **paresseux** : un tour dont l'échéance est passée est passé au
prochain `JoinCombat` ou à la prochaine commande. `GET /api/v1/combats/{id}`
donne une lecture seule.

À la fin, Combat rapporte l'issue à Dungeon (`DungeonCombatService/
ReportCombatResult`, `command_id` = `combat_id`) hors transaction. Un échec
laisse le rapport en attente ; `PendingCombatReportsWorker` le relance.

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
| `CombatInstances` | Identifiants, rencontre, run, étage, statut, résultat, tour/manche, ordre d'initiative, acteur et échéance du tour, journal d'événements, commandes traitées, date du rapport à Dungeon, version de ligne (`xmin`) |
| `CombatFighters` | Identité, ordre, état courant (PV, mana, statistiques, défense), état initial, capacités (code, coût, puissance), objets, effets, visuel (codes) |

`CombatCommandId` et `CombatEncounterId` sont uniques en base. Deux commandes
concurrentes sur un même combat : la seconde est refusée (`conflict`).

## Erreurs

| Exception | HTTP | gRPC |
|---|---:|---|
| `HeroNotFoundException` | 404 | `NotFound` |
| `ExternalServiceUnavailableException` | 503 | `Unavailable` |
| `InvalidCombatEncounterException` | 422 | `FailedPrecondition` |
| `IdempotencyKeyReuseException` | 409 | `AlreadyExists` |
| `ValidationException` (0 monstre, doublon, étage…) | 422 | `InvalidArgument` |

## Tester la création d'un combat

### Avec la stack Docker

```bash
docker compose up -d --build
```

L'API écoute sur `http://localhost:8080`, le gRPC interne sur
`http://localhost:8081`, PostgreSQL sur `5433` et RabbitMQ sur `15672`.

```bash
# Un combat BOSS (E = id de la rencontre)
E=$(uuidgen)
grpcurl -plaintext \
  -import-path Combat.Contracts/Protos \
  -proto combat_combat_v1.proto \
  -d "{\"session_id\":\"$(uuidgen)\",\"encounter_id\":\"$E\",\"command_id\":\"$E\",\"encounter_type\":\"ENCOUNTER_TYPE_BOSS\",\"run_id\":\"$(uuidgen)\",\"floor\":1,\"participants\":[{\"hero_id\":\"00000000-0000-0000-0000-000000000101\"}],\"monsters\":[{\"element_id\":\"$(uuidgen)\",\"is_boss\":true}]}" \
  localhost:8081 combat.v1.CombatService/CreateCombat
```

Types utilisés par Dungeon : `ENCOUNTER_TYPE_ENEMY` et `ENCOUNTER_TYPE_BOSS`.
Le héros doit exister dans Player (ici le guerrier de démonstration).

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
