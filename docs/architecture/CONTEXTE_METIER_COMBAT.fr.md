# Contexte métier Combat

> Document de cadrage. Le modèle et les règles ci-dessous décrivent la cible
> attendue ; le schéma n’est pas implémenté par ce document.

## Décisions et frontières

Combat résout un affrontement complet. Il récupère les données nécessaires des
héros et les informations de configuration des monstres, crée des snapshots
temporaires, génère les monstres et leurs capacités, ordonne les tours, valide
les actions, gère leur échéance, applique les effets et produit un résultat
final. Il ne modifie pas directement les données persistantes de Player,
Dungeon, Rewards ou Progression.

Player reste propriétaire de l’identité, des héros, de la session et du
roster. Dungeon reste propriétaire du `DungeonRun`, de la génération du donjon,
des salles, des positions et du mouvement. Rewards possède l’inventaire, les
achats, les échanges et les récompenses persistantes. Progression possède les
statistiques, l’historique, l’XP et le leaderboard ; le nom d’« achievements »
doit être rapproché de cette responsabilité. Les identifiants externes sont
des références de contrat, pas des clés étrangères vers une base distante.

Le meeting du 02/09 prévoit que Dungeon utilise une seed pour la génération et
gère les positions, les points de vie et le mana utiles pendant le run. Le
mouvement avance d’une case par tour. Combat génère les monstres à la demande,
récupère les informations des héros et les types de monstres, autorise une
action par tour et renvoie l’état final à Dungeon. Ces responsabilités doivent
rester compatibles avec les [ADR-GLOB-005](../adr/ADR-GLOB-005-distributed-data-consistency.md)
et [ADR-GLOB-011](../adr/ADR-GLOB-011-player-game-session-and-dungeon-run-ownership.md).

## Modèle conceptuel proposé

Les noms et champs sont conservés tels quels pour permettre une comparaison
avec les échanges futurs. Les types `UUID`, `String`, `Integer` et `Bool` sont
des types conceptuels ; aucune table, migration ou implémentation n’est
impliquée.

| Concept | Champs exacts |
|---|---|
| `FIGHT` | `UUID fight_id`, `String status`, `String result`, `String difficulty`, `Integer timeCombat` |
| `FIGHTER` | `UUID fighter_id`, `String type`, `String external_id`, `Integer level`, `Integer current_hp`, `Integer max_hp`, `Integer current_mana`, `Integer max_mana`, `Integer attack`, `Integer defense`, `Integer speed`, `String state` |
| `MONSTER` | `UUID monster_id`, `Bool IsBoss`, `Integer base_hp`, `base_attack`, `base_defense`, `base_speed` |
| `ABILITY` | `UUID ability_id`, `String name`, `Integer mana_cost`, `String target_type` |
| `TURN` | `UUID turn_id`, `Integer turn_number`, `timeMax`, `String status` |

Relations fournies par le modèle :

| Relation | Cardinalité à conserver |
|---|---|
| `FIGHT` — `FIGHTER` | `FIGHT 1 -- 1..* FIGHTER` |
| `MONSTER` — `FIGHTER` | `MONSTER 0..* -- 0..1 FIGHTER` |
| `FIGHTER` — `ABILITY` | `FIGHTER 0..* -- 0..* ABILITY` |
| `FIGHT` — `TURN` | `FIGHT 1 -- 0..* TURN` |
| `FIGHTER` — `TURN` | `FIGHTER 0..* -- 1 TURN` |

Les deux relations impliquant `MONSTER` et `TURN` sont ambiguës si `MONSTER`
est un catalogue ou si `TURN` représente un tour partagé ou une affectation.
La cardinalité est donc reprise littéralement et ne doit pas être transformée
silencieusement. La correction proposée séparément est de préciser la
direction, la nature catalogue/instance et, pour `FIGHTER`–`ABILITY`, une
éventuelle association d’usage. Il manque aussi la représentation des actions
et tentatives, des effets/statuts et des liens explicites avec `fight_id`.

## Cycle de combat

À l’initialisation, Combat obtient au minimum le héros, ses PV/mana, ses
statistiques, ses capacités disponibles et les données d’inventaire strictement
nécessaires. Il obtient aussi les types de monstres, les quantités et la
difficulté. Une donnée obligatoire absente empêche le démarrage ; une donnée
non nécessaire ne le bloque pas. Les mocks doivent suivre le même format que
les données réelles.

Combat crée un snapshot temporaire du héros, des monstres générés et des
statistiques temporaires. Les monstres ont un identifiant unique dans le
combat, un état initial et des capacités compatibles avec leur type et leur
niveau. La génération doit respecter exactement les quantités reçues ; un
type inconnu ou une quantité invalide est une erreur, et une quantité totale
nulle ne crée aucun combat. La règle d’effet de la difficulté sur les PV et
les dégâts, ainsi que la capacité par défaut lorsqu’aucune capacité valide
n’existe, restent des propositions à confirmer.

Le moteur détermine le premier participant selon une règle d’initiative à
arrêter (vitesse, initiative dédiée ou ordre fixe sont des propositions). Un
seul participant possède le tour actif ; un participant hors combat ne reçoit
plus de tour ; aucun tour n’est créé après un état terminal.

Une tentative vérifie l’acteur, le tour actif, la capacité, la cible et les
conditions d’utilisation. Une tentative refusée ne modifie pas l’état et ne
consomme pas le tour. Une tentative acceptée applique au plus une action,
met à jour le snapshot, puis termine le tour. Une même commande ne doit pas
produire deux effets après double clic, retry, replay, timeout ou redélivrance.
La clé d’idempotence ou le `CommandId` doit permettre de retrouver le résultat
précédent. La concurrence entre tentative et échéance du timer reste à
spécifier.

Le timer est côté serveur, configurable et mockable. Chaque tour expose une
deadline UTC au client ; une action reçue après cette deadline est refusée.
Une action par défaut à l’échéance est une règle proposée à confirmer.
`timeMax` représente la durée maximale prévue du tour ; son unité, le
comportement après redémarrage et la politique de reprise restent ouverts. Le
snapshot initial est immuable après l’initialisation ; le snapshot courant est
modifié par les effets acceptés. Il faut tracer les transitions et échéances,
sans produire un log pour chaque tick.

Les mises à jour possibles comprennent les PV, le mana, les effets, les
statuts et les potions prévues par l’architecture. `current_hp` ne doit jamais
dépasser `max_hp`. À zéro PV, le combattant devient hors combat et ne peut plus
agir. Combat produit l’état final et le résultat de chaque héros ou participant
du groupe ; le KO individuel ne termine le combat que si les conditions de fin
du groupe sont atteintes. Quand ces conditions de victoire ou de défaite sont
atteintes, Combat enregistre le résultat, passe à un état terminal et refuse les
nouvelles actions. Les valeurs exactes de `status` et `result` restent à fixer ;
`INITIALIZING`, `ACTIVE`, `FINISHED`, `FAILED` et `VICTORY`, `DEFEAT` sont des
propositions de vocabulaire.

## Résultat et échanges

Le résultat final doit pouvoir transporter au minimum l’identifiant du combat,
le héros, le résultat, les PV et mana finaux, les modifications liées au
combat et les données nécessaires au post-combat. Combat utilise les appels
gRPC et événements déjà décrits dans l’architecture : snapshots Player et
Rewards à l’initialisation, récompense auprès de Rewards en cas de victoire,
et résultat auprès de Dungeon. L’échec de transmission est explicite,
journalisé et réessayable avec la même clé lorsque cela est sûr ; il ne vaut
jamais succès. Les faits statistiques Progression concernent les actions
résolues et le combat terminé, pas les tentatives refusées.

La clarification du meeting exige d’enrichir les contrats pour transmettre les
PV/mana courants de Dungeon à Combat et les états finaux de Combat à Dungeon.
Les champs, événements, compatibilités et propriétaires de chaque évolution
devront être définis dans les contrats concernés, avec une version explicite
conforme à l’[ADR-GLOB-007](../adr/ADR-GLOB-007-protobuf-contract-packages.md).

## Journalisation et traces obligatoires

Tous les événements métier et techniques utiles sont des logs structurés
Serilog, avec traces OpenTelemetry conformément à l’[ADR-GLOB-009](../adr/ADR-GLOB-009-distributed-observability.md)
et à l’[ADR-0022](../adr/0022-use-serilog-for-structured-application-logging.md).
Chaque entrée porte un horodatage UTC, un nom d’événement et un niveau
`Info`, `Warn`, `Error` ou `Debug`. Les événements requis doivent être
collectés sans sampling qui les supprime. Les transitions et succès ou refus
métier attendus sont en `Info`, les anomalies et retries en `Warn`, les échecs
en `Error` et les détails supplémentaires en `Debug`.

Le contexte inclut les identifiants disponibles : `FightId` et la
correspondance à préciser avec `CombatId`, `FighterId`/`HeroId`, `TurnId`,
`CommandId`, `messageId`, `session`, `run`, `encounter`, `CorrelationId`,
`TraceId` et `CausationId`. Selon l’événement, ajouter le motif, la cible, la
capacité, la durée et l’issue. Séparer toujours la tentative reçue de l’effet
effectivement appliqué.

À couvrir :

- cycle du fight, initialisation, chaque lecture de dépendance, chaque lecture
  de snapshot reçue, chaque snapshot obtenu et le résultat de chaque appel ;
- génération des monstres et attribution des capacités ;
- début et fin de tour, deadline, timeout et transitions d’état ;
- toutes les tentatives reçues, acceptées, refusées, rejouées et en erreur ;
- changements de PV, mana, effets et inventaire, avec delta utile avant/après ;
- résultats `pending`, échecs, retries et acknowledgements ;
- appels gRPC et messages RabbitMQ reçus, publiés, traités, rejoués ou
  dédupliqués ;
- connexions, reconnexions, resynchronisations et refus d’accès.

Les logs opérationnels sont distincts de l’historique métier durable. Aucun
secret, token ou dump complet ne doit être journalisé ; les payloads sont
ciblés. Le stockage, la rétention et le comportement en cas d’indisponibilité
du collecteur restent à décider. Les événements de Progression portent les
actions résolues et les faits confirmés, tandis que l’observabilité conserve
les tentatives nécessaires au diagnostic.

## Points ouverts à confirmer

- Cardinalités `MONSTER`–`FIGHTER` et `FIGHTER`–`TURN`, et association
  `FIGHTER`–`ABILITY` ;
- modèle des actions/tentatives, identifiants d’idempotence, concurrence avec
  le timer et comportement en cas de reprise ;
- deadline UTC, unité de temps, `timeMax` et règle d’initiative ;
- références exactes entre fight, combat, session, run et encounter ;
- snapshots initial/courant, usages et effets des capacités, inventaire et
  modifications post-combat ;
- conditions de groupe, états terminaux et séquence post-combat ;
- nom du domaine responsable des achievements et stockage/rétention des logs.
