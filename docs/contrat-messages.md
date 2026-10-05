# Contrat de messages entre Vue et Unity

Ce document décrit les messages que la page Vue et le build Unity WebGL s'envoient.
Il fait référence. Pour changer un message, on modifie d'abord ce fichier dans une PR
relue par les deux autres membres du groupe, puis le code des deux côtés.

Le code qui implémente le contrat se trouve dans ces fichiers :

| Côté  | Fichier                                                                     |
| ----- | --------------------------------------------------------------------------- |
| Vue   | `web/src/types/bridge.ts` et `web/src/composables/useUnity.ts`              |
| Unity | `unity/Assets/Scripts/Bridge/BridgeMessages.cs` et `WebBridge.cs`           |
| JS    | `unity/Assets/Plugins/WebGL/AGL3DBridge.jslib`                              |

## Format d'un message

Chaque message est un objet JSON avec un type et un contenu :

```json
{ "type": "<nom>", "payload": { ... } }
```

## Envoi d'un message

Pour envoyer un message à Unity, Vue appelle
`unityInstance.SendMessage('WebBridge', 'Receive', json)`. Côté Unity, la méthode
`WebBridge.Receive(string json)` reçoit tous les messages. Elle lit le type, convertit
le contenu, puis déclenche l'événement C# correspondant, par exemple `InitReceived` ou
`SetPointsReceived`. Les autres scripts Unity s'abonnent à ces événements. Le GameObject
qui porte `WebBridge` doit s'appeler exactement `WebBridge`.

Pour envoyer un message à Vue, un script Unity appelle `WebBridge.Emit(type, payload)`.
Cette méthode appelle la fonction JavaScript `AGL3D_Emit` du fichier `.jslib`, qui
déclenche un événement DOM `agl3d` sur le canvas Unity. `useUnity` écoute cet
événement. Dans l'Éditeur Unity, `Emit` écrit seulement le message dans la Console.

## Limites de JsonUtility

Unity lit le JSON avec JsonUtility, qui impose trois règles :

- Le contenu d'un message est toujours un objet, jamais un tableau. C'est pour ça que
  `setPoints` envoie `{ "points": [...] }`.
- Une chaîne absente ou `null` arrive vide côté Unity. Les nombres et les booléens ne
  peuvent pas être `null`.
- Les noms de champs sont en camelCase et identiques des deux côtés.

## Conventions

Une clé d'élément a la forme `<piece>.<element>` en kebab-case, par exemple
`salon.mur-ouest`. Unity la stocke dans `InspectableElement.key`, et le JSON de l'état
des lieux dans `Element.key`. On ne renomme jamais une clé sans mettre à jour les deux.

Une clé de pièce vaut `salon`, `chambre` ou `cuisine`. La valeur `overview` désigne la
vue d'ensemble de l'appartement.

Le statut d'un point vaut `inchange`, `degrade`, `ameliore`, `releve` ou `neutre`. On
utilise `neutre` quand la page affiche seulement l'entrée ou seulement la sortie.

Les coordonnées écran vont de 0 à 1 dans le canvas, avec l'origine en haut à gauche.
Elles ne dépendent pas de la densité de pixels de l'écran. Pour placer une carte à
côté du point cliqué, Vue les multiplie par la largeur et la hauteur du canvas.

## Démarrage

1. Vue charge le build avec `createUnityInstance`.
2. Unity démarre. Chaque `InspectableElement` s'enregistre.
3. `WebBridge` envoie `ready` avec la liste des clés.
4. Vue envoie `init`, puis `setPoints`.
5. L'utilisateur interagit. Les deux côtés échangent des messages.
6. Quand le composant Vue disparaît, Vue appelle `Quit()` pour arrêter Unity.

Vue n'envoie rien avant d'avoir reçu `ready`. À la réception de `ready`, Vue peut
comparer les clés reçues à celles du JSON et signaler dans la console celles qui
manquent d'un côté ou de l'autre.

## Messages de Vue vers Unity

| Type          | Contenu                                                   | Ce que fait Unity |
| ------------- | --------------------------------------------------------- | ----------------- |
| `init`        | `{ dossierId: string, mode: "entree" \| "sortie" \| "comparer" }` | Remet à zéro la sélection et place la caméra sur la vue d'ensemble. |
| `setPoints`   | `{ points: [{ key, numero, label, statut }] }`            | Remplace tous les marqueurs. Chaque point reçoit un marqueur numéroté, coloré selon son statut. Unity ignore une clé inconnue et affiche un avertissement. |
| `focusRoom`   | `{ room: string }`, une clé de pièce ou `"overview"`      | Déplace la caméra vers la pièce, puis envoie `roomChanged`. |
| `select`      | `{ key: string \| null }`                                 | Met l'élément en surbrillance. `null` ou une chaîne vide retire la sélection. Unity n'envoie pas `pointSelected` en réponse, pour éviter une boucle. |
| `setContrast` | `{ enabled: boolean }`                                    | Active ou désactive le contraste élevé, avec des couleurs de statut plus distinctes et des étiquettes plus lisibles. |

Exemples :

```json
{ "type": "setPoints", "payload": { "points": [
  { "key": "salon.mur-ouest", "numero": 1, "label": "Mur ouest", "statut": "degrade" },
  { "key": "salon.parquet",   "numero": 2, "label": "Parquet",   "statut": "inchange" }
] } }
{ "type": "focusRoom", "payload": { "room": "cuisine" } }
{ "type": "select",    "payload": { "key": "cuisine.evier" } }
```

## Messages de Unity vers Vue

| Type            | Contenu                                  | Quand Unity l'envoie |
| --------------- | ---------------------------------------- | -------------------- |
| `ready`         | `{ keys: string[] }`                     | Une seule fois, au démarrage de `WebBridge`, avec les clés de tous les `InspectableElement` actifs. |
| `pointSelected` | `{ key: string, x: number, y: number }`  | Quand l'utilisateur clique ou touche un élément ou son marqueur. |
| `roomChanged`   | `{ room: string }`                       | Quand la caméra a fini de se déplacer, après un `focusRoom` ou quand l'utilisateur change de pièce en naviguant librement. |

Exemple :

```json
{ "type": "pointSelected", "payload": { "key": "salon.mur-ouest", "x": 0.42, "y": 0.61 } }
```

## Modifier le contrat

1. Ouvrez une PR qui modifie ce fichier, `bridge.ts` et `BridgeMessages.cs` ensemble.
2. Ajouter un message ou un champ optionnel ne casse pas l'autre côté.
3. Renommer ou supprimer un message ou un champ casse l'autre côté. Prévenez l'équipe
   et mettez à jour Vue et Unity dans la même PR.
