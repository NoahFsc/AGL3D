# Plan d'action

On découpe le projet en trois lots, un par personne. Chaque lot contient une partie
Unity et une partie Vue, pour que tout le monde travaille sur les deux. Les lots
dépendent peu les uns des autres.

Le planning couvre 6 semaines à partir du lundi 5 octobre 2026. On le recalera sur les
dates de rendu du cours.

## Lot A. Maquette, caméra et navigation par pièce

Côté Unity :

- Modéliser l'appartement avec ProBuilder : salon, chambre, cuisine, murs, sols,
  ouvertures, et le mobilier des points (table, placard, évier, compteur).
- Poser un `InspectableElement` et un collider sur les éléments des 6 points, avec les
  clés du JSON.
- Faire tourner, zoomer et déplacer la caméra autour de l'appartement, à la souris et
  au doigt, dans des limites fixées.
- Définir une position de caméra par pièce et une pour la vue d'ensemble, avec une
  transition animée.
- Recevoir `focusRoom` et envoyer `roomChanged`.

Côté Vue :

- Ajouter une barre de boutons : Vue d'ensemble, Salon, Chambre, Cuisine.
- Envoyer `focusRoom` au clic.
- Mettre à jour la pièce courante à la réception de `roomChanged`.

Livrables : les prefabs `Appartement` et `CameraRig`, le composant `RoomButtons.vue`.

## Lot B. Pont entre Vue et Unity, sélection et fiche détail

Côté Unity :

- Terminer `WebBridge`, dont le squelette existe déjà, avec la gestion d'erreurs et les logs.
- Détecter l'élément sous le clic ou le doigt et envoyer `pointSelected`.
- Mettre en surbrillance l'élément reçu par `select`.
- Créer la scène principale `Main.unity` et son GameObject `WebBridge`.

Côté Vue :

- Terminer `useUnity` : mise en attente des messages avant `ready`, erreurs de
  chargement, progression.
- Terminer `JsonRepository`, avec la validation du JSON et la comparaison des clés
  reçues dans `ready` avec celles du JSON.
- Créer le composant qui affiche le canvas Unity et son chargement.
- Créer la fiche détail : élément, pièce, constats, état, commentaire, relevé et photos,
  avec une visionneuse.

Livrables : la scène `Main.unity`, les composants `UnityViewer.vue`, `DetailCard.vue`
et `PhotoViewer.vue`, une page de démo qui marche de bout en bout.

## Lot C. Marqueurs, statuts et modes Entrée, Sortie, Comparer

Côté Unity :

- Afficher un marqueur numéroté au-dessus de chaque élément. Le marqueur fait face à
  la caméra et on peut cliquer dessus.
- Afficher les étiquettes avec TextMeshPro, à taille constante à l'écran, et les
  masquer quand un mur les cache.
- Colorer les marqueurs selon le statut, avec une palette normale et une palette à
  contraste élevé.
- Recevoir `setPoints` et `setContrast`.

Côté Vue :

- Ajouter le choix du mode : Entrée, Sortie ou Comparer.
- Calculer le statut de chaque point à partir des deux états des lieux.
- Envoyer `setPoints` à chaque changement de mode, et `setContrast` depuis un
  interrupteur.
- Afficher le nombre de points par statut et la liste des points. Un clic dans la liste
  envoie `select`.

Livrables : le prefab `PointMarker`, les composants `ModeSwitch.vue`,
`SummaryCounters.vue` et `PointList.vue`, le module `comparaison.ts`.

## Dépendances

Le contrat de messages bloque tout le reste. On le valide à trois en semaine 1.
Ensuite :

- La maquette du lot A permet au lot B de tester la sélection sur les vrais objets, et
  au lot C de placer les marqueurs.
- Le pont du lot B et la scène `Main.unity` permettent aux lots A et C de tester dans
  la page.
- Le calcul des statuts du lot C permet de tester `setPoints` dans Unity.

Pour ne pas s'attendre les uns les autres :

- Toute évolution du contrat passe par une PR sur `docs/contrat-messages.md`.
- Avant que la maquette existe, les lots B et C travaillent sur une scène de test avec
  des cubes qui portent un `InspectableElement` et les vraies clés du JSON.
- Côté Vue, une fausse version de `useUnity` écrit les messages envoyés dans la console
  et simule `ready` et `pointSelected`. On peut ainsi développer l'interface sans build
  WebGL.

## Jalons

| Semaine | Lot A | Lot B | Lot C | Objectif commun |
| ------- | ----- | ----- | ----- | --------------- |
| S1 | Plan coté de l'appartement, premier volume du salon | Relecture du contrat, `Main.unity` et `WebBridge`, premier build affiché dans Vue | Maquette de l'interface des modes et des couleurs de statut | Contrat validé, réglages Web faits, un cube Unity visible dans la page |
| S2 | Les 3 pièces en volumes simples, `InspectableElement` posés sur les 6 éléments | `ready` et `pointSelected` de bout en bout, mise en attente dans `useUnity` | Marqueur sur la scène de test, `setPoints` reçu | Un clic dans la 3D apparaît dans la console de la page |
| S3 | Caméra, positions par pièce, `focusRoom` et `roomChanged`, boutons de pièces | `JsonRepository` terminé, fiche détail sans photos | Couleurs par statut, calcul des statuts, choix du mode | Démo intermédiaire : changer de pièce, cliquer un élément, voir sa fiche |
| S4 | Matériaux, éclairage, mobilier | Photos et visionneuse, surbrillance sur `select` | `setContrast`, compteurs, liste des points | Toutes les fonctionnalités en place, plus d'ajout après cette semaine |
| S5 | Taille du build, nombre d'appels de rendu, tactile | Erreurs, états de chargement, accessibilité de la fiche | Lisibilité des étiquettes, contraste, clavier | Chacun teste le lot d'un autre |
| S6 | Corrections | Corrections, notes pour l'intégration Laravel | Corrections | Rendu : build final, README, soutenance |

## Règles pour éviter les conflits dans Unity

Git fusionne mal les scènes et les prefabs Unity. On suit donc ces règles :

1. La scène principale `Assets/Scenes/Main.unity` a un seul propriétaire, le lot B. Les
   autres ne la modifient pas. Ils demandent au propriétaire d'y ajouter leur prefab,
   ou ouvrent une PR dédiée après l'avoir prévenu.
2. Chacun travaille dans ses propres prefabs et sa propre scène de test :
   - lot A dans `Assets/Lot-A/`, avec `Appartement`, `CameraRig` et `Lot-A_Sandbox.unity` ;
   - lot B dans `Assets/Lot-B/` et `Assets/Scripts/Bridge/` ;
   - lot C dans `Assets/Lot-C/`, avec `PointMarker`, les matériaux de statut et
     `Lot-C_Sandbox.unity`.

   On place des prefabs dans la scène plutôt que des objets isolés.
3. Tout le monde configure UnityYAMLMerge avec la commande du README. Le fichier
   `.gitattributes` l'utilise déjà pour les scènes et les prefabs.
4. On prévient l'équipe sur le canal du groupe avant de modifier `ProjectSettings/` ou
   `Packages/manifest.json`. La modification part dans une PR courte, séparée, qu'on
   merge vite.
5. Un fichier `.meta` se commite toujours avec son fichier, et se supprime avec lui. On
   crée, déplace et renomme les assets depuis l'Éditeur Unity, jamais depuis le Finder.
6. On prévient avant de travailler sur une scène ou un prefab partagé. En cas de conflit
   sur un `.unity` ou un `.prefab`, on garde la version d'une personne et l'autre refait
   sa modification. On ne fusionne pas ces fichiers à la main.
7. On ne commite jamais `Library/`, `Temp/`, `Logs/` ni `UserSettings/`. Le fichier
   `unity/.gitignore` les exclut.

## Branches et PR

- `main` marche toujours : Unity ouvre le projet sans erreur et `npm run build` passe.
  Personne ne pousse directement sur `main`.
- Une branche par tâche, nommée d'après le lot : `lot-a/camera-presets`,
  `lot-b/picking`, `lot-c/markers`. On utilise aussi `contrat/...`, `fix/...` et
  `docs/...`.
- Les messages de commit sont en français, au présent, avec un préfixe : `feat`, `fix`,
  `docs`, `chore` ou `refactor`. Par exemple :
  `feat(lot-c): couleurs des marqueurs par statut`.
- Une PR représente au plus une journée de travail. On la rebase sur `main` avant le
  merge.
- Une autre personne relit chaque PR. Deux personnes relisent celles qui touchent au
  contrat, à `Main.unity` ou à `ProjectSettings/`.
- La description de la PR dit ce qui change et comment le tester dans Unity, dans Vue,
  ou les deux. On ajoute une capture ou un GIF pour un changement visuel.
- On merge en squash et on supprime la branche.

## Quand une tâche est terminée

- [ ] Pour une tâche Unity, le résultat se voit dans le build WebGL affiché par la page,
      pas seulement dans le mode Play de l'Éditeur.
- [ ] La tâche respecte le contrat de messages. Si le contrat change,
      `docs/contrat-messages.md`, `bridge.ts` et `BridgeMessages.cs` changent dans la
      même PR.
- [ ] La Console Unity n'affiche aucune nouvelle erreur ni nouvel avertissement. Chaque
      fichier ajouté a son `.meta` commité. Le diff ne contient aucune modification de
      scène ou de réglage faite par erreur.
- [ ] `npm run lint`, `npm run type-check` et `npm run build` passent. Aucun `fetch` ne
      se trouve hors d'un `EdlRepository`.
- [ ] On a testé sur Chrome et Firefox, en largeur desktop et en largeur mobile.
- [ ] Une autre personne a relu et approuvé la PR. Elle est mergée dans `main` et la
      branche est supprimée.
- [ ] Le README ou la documentation sont à jour si la tâche change l'installation ou
      l'usage.
