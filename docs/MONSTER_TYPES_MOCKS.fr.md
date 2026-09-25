# Types de monstres simulés

Le backend expose une source de développement pour consulter un catalogue de
types de monstres par HTTP. La route est versionnée sous
`/api/v1/monster-types` et renvoie les champs `id`, `name`, `isBoss`,
`baseHealth`, `baseAttack`, `baseDefense` et `baseSpeed`.

## Activation

La source simulée est activée uniquement lorsque l'application s'exécute dans
l'environnement `Development` et que `MonsterTypeMock:Enabled` vaut `true`.
Le fichier `Combat.Presentation/appsettings.Development.json` active ce
réglage, tandis que `Combat.Presentation/appsettings.json` le désactive. La
variable d'environnement correspondante est `MonsterTypeMock__Enabled`.

Quand le mock n'est pas activé, aucune source réelle n'est encore fournie.
L'application utilise alors une source indisponible et les appels au catalogue
répondent avec l'erreur `503 Service Unavailable` décrite plus bas.

## Catalogue fourni

Le mock fournit exactement les trois types suivants. Les valeurs correspondent
au modèle applicatif et sont renvoyées telles quelles par l'API.

| Identifiant | Nom | Boss | Vie de base | Attaque de base | Défense de base | Vitesse de base |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `a1111111-1111-1111-1111-111111111111` | Cave Rat | non | 18 | 5 | 1 | 12 |
| `b2222222-2222-2222-2222-222222222222` | Goblin Raider | non | 42 | 11 | 5 | 9 |
| `c3333333-3333-3333-3333-333333333333` | Stone Guardian | oui | 180 | 22 | 18 | 4 |

Le catalogue est validé avant d'être exposé. Il doit contenir au moins un type,
chaque définition doit respecter ses contraintes de modèle et les identifiants
doivent être uniques. Une erreur de catalogue invalide la réponse entière.

## Endpoints

`GET /api/v1/monster-types` renvoie `200 OK` avec un tableau contenant les
trois définitions dans l'ordre du catalogue ci-dessus.

`GET /api/v1/monster-types/{monsterTypeId}` renvoie `200 OK` avec la définition
correspondant à l'identifiant demandé. Cet endpoint unitaire ne reçoit qu'un
identifiant dans le segment de route.

L'interface applicative `IMonsterTypeCatalog.ResolveRequiredAsync` permet en
revanche de résoudre une sélection de plusieurs identifiants. Elle vérifie la
sélection avant de renvoyer le résultat : les GUID vides et les doublons sont
rejetés, et la présence d'un identifiant inconnu rejette toute la sélection.
Cette opération atomique pourra être consommée par le futur générateur.

## Réponses d'erreur

Les erreurs utilisent la structure Problem Details. Les réponses `503` et les
erreurs de catalogue `500` forcent le type `application/problem+json` ; les
réponses de validation et de ressource absente suivent le format JSON Problem
Details configuré par le middleware.

| Situation | Statut | Titre |
| --- | ---: | --- |
| Identifiant fourni qui n'est pas un GUID ou qui est égal au GUID vide | `422` | `Validation error` |
| Identifiant bien formé mais absent du catalogue | `404` | `Resource not found` |
| Aucune source de types de monstres n'est disponible | `503` | `Monster type source unavailable` |
| Source présente mais catalogue invalide | `500` | `Invalid monster type catalog` |
| Exception non traitée | `500` | `Internal server error` |

La réponse `422` contient les erreurs de validation par propriété. La réponse
`404` indique l'identifiant inconnu dans son détail. Pour un catalogue invalide,
la réponse `500` expose aussi l'objet `errors` associé aux entrées invalides ou
aux identifiants dupliqués. Le détail d'une exception non traitée est renseigné
en `Development` et masqué dans les autres environnements.

## Limites

Le catalogue décrit ici est une donnée de développement stable. Il ne constitue
pas une source métier persistée et aucune source réelle n'est actuellement
branchée. Le futur générateur n'est pas encore branché sur ce catalogue.
