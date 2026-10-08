# Plan d'action

Ce plan suit le [cahier des charges](cahier-des-charges.pdf). On découpe le projet en
trois lots, un par personne. Chaque lot contient une partie Unity et une partie Vue,
pour que tout le monde travaille sur les deux.

Le planning couvre 6 semaines à partir du lundi 5 octobre 2026. On le recalera sur les
dates de rendu du cours.

## Ce que demande le cahier des charges

L'utilisateur inspecte un appartement en 3D. Il clique sur un élément, lui donne un
état, et un repère coloré apparaît à cet endroit. Il refait la même inspection à la
sortie du locataire. En mode Comparer, les éléments dégradés passent au rouge dans la 3D.

On livre uniquement la version WebGL, affichée dans la page Vue. Il n'y a pas de version
desktop. L'utilisateur se sert de la souris et du clavier. Le tactile n'est pas demandé.

### Fonctionnalités, par ordre d'importance

Si le temps manque, on retire d'abord les dernières de la liste.

1. Inspecter un élément. Le survol l'illumine, le clic ouvre sa fiche.
2. Indiquer son état. L'utilisateur choisit un état, valide, et un repère coloré
   apparaît dans la 3D.
3. Comparer l'entrée et la sortie. Les éléments dégradés passent progressivement au rouge.
4. Naviguer dans l'appartement. Tourner, zoomer et changer de pièce.
5. Relever les compteurs. L'utilisateur saisit les index, l'application calcule la
   consommation.
6. Aller directement à un défaut. En mode Comparer, un clic dans la liste déplace la
   caméra vers l'élément.

### Contrôles

| Action | Résultat |
| ------ | -------- |
| Clic droit maintenu et glisser | La caméra tourne autour de l'appartement. |
| Molette | Zoom avant ou arrière. |
| Clic sur Salon, Chambre ou Cuisine | La caméra se déplace vers la pièce en environ une seconde. |
| Survol d'un élément | L'élément s'illumine, dans une couleur différente de celles des états. |
| Clic sur un élément | Sa fiche s'ouvre et la caméra se rapproche de lui. |
| Choix d'un état et validation | Un repère coloré apparaît sur l'élément. |
| Clic sur un repère | La fiche s'ouvre pour modifier le constat. |
| Clic sur un compteur | Une fiche s'ouvre pour saisir l'index. |
| Échap, ou clic dans le vide | La fiche se ferme. |

### Appartement et caméra

- Trois pièces présentées comme une maquette, avec des murs à mi-hauteur : un salon qui
  contient l'entrée et les compteurs, une chambre et une cuisine.
- 10 éléments à inspecter. Les compteurs sont près de l'entrée.
- Trois vues de caméra : l'appartement entier, une pièce, un élément.
- La caméra reste au-dessus des murs et ne traverse pas les cloisons.

### Écran d'accueil

- La maquette entière tourne lentement sur elle-même.
- Le nom du logement s'affiche en haut de l'écran.
- Trois boutons : Entrée, Sortie et Comparer. Entrée est mis en avant. Sortie et
  Comparer restent désactivés tant que l'état des lieux d'entrée n'est pas fait.
- Après un clic sur Entrée, la caméra va au salon et un message s'affiche : "Survolez un
  élément, cliquez pour le noter."

### Style

- Aspect de maquette d'architecte : formes simples, couleurs douces, murs et sol
  neutres. Inspirations citées : Les Sims en mode construction, Unpacking et Monument
  Valley.
- Couleurs des états : vert pour Bon, orange pour Usage, rouge pour Mauvais. Le cahier
  des charges cite aussi Neuf, mais l'équipe a choisi de ne garder que ces trois états.
- Éclairage doux, peu d'ombres, pas d'effets lourds.
- Fiches blanches et grandes, texte lisible, icônes simples.
- Pas de chronomètre ni de score. Chaque action a un retour visuel ou sonore.
- Les assets gratuits sont autorisés pour le prototype.

## Décisions de l'équipe

- **Trois états.** On garde Bon, Usage et Mauvais. On ne garde pas Neuf.
- **10 éléments.** Le fichier de démo contient ces éléments :
  - salon : porte d'entrée, compteur d'eau, compteur d'électricité, mur, sol ;
  - chambre : mur, sol, placard ;
  - cuisine : évier, plan de travail.
- **Enregistrement des constats.** L'utilisateur enregistre ses constats. Sans serveur, on
  les garde dans le `localStorage` du navigateur. Un bouton charge un jeu de démo pour
  la soutenance.
- **Pas de contraste élevé.** `setContrast` est retiré du contrat et du code.

Il reste un point à trancher. Le tableau des contrôles du cahier des charges ouvre la
fiche au survol, alors que la liste des fonctionnalités l'ouvre au clic. On propose de
suivre la liste : le survol illumine, le clic ouvre la fiche.

## Où on en est au 5 octobre 2026

### Ce qui marche

La page Vue charge le build Unity WebGL et reçoit ses messages. Dans la scène de test
`SampleScene`, un cube porte un `InspectableElement` avec la clé `salon.mur-ouest`. Au
démarrage, Unity envoie `ready {"keys":["salon.mur-ouest"]}` et la page l'affiche.

Le dépôt contient déjà :

- le projet Unity dans `unity/` et l'application Vue dans `web/` ;
- les scripts du pont : `WebBridge`, `InspectableElement`, `BridgeMessages` et le
  fichier `AGL3DBridge.jslib` ;
- les réglages Web du projet et le build profile `Web`, avec une sortie dans
  `web/public/unity/` ;
- côté Vue, le composable `useUnity`, le composant `UnityViewer.vue` et une page de test
  qui liste les messages reçus d'Unity ;
- les types TypeScript, avec les états `bon`, `usage` et `mauvais` ;
- `EdlRepository` et `JsonRepository`, qui lisent les données de référence et
  enregistrent les constats dans le `localStorage` ;
- le fichier de démo `appart-demo.json`, avec les 10 éléments et un jeu de démo
  d'entrée et de sortie ;
- le contrat de messages à jour, le README et ce plan.

### Ce qui manque dans le code actuel

- Aucun script Unity ne réagit encore aux messages reçus. `WebBridge` les reçoit, puis
  affiche un avertissement "Aucun abonné".
- `useUnity` ignore les messages envoyés avant `ready` au lieu de les mettre en attente.
- La caméra est fixe.
- La scène principale `Main.unity` n'existe pas. On teste dans `SampleScene`.
- Le build dans `web/public/unity/` date d'avant la mise à jour du contrat. Il faut le
  refaire.
- Le fichier de démo ne référence aucune photo. Le champ `photo` existe dans les
  constats, mais vaut `null`.

### Ce qu'il reste à faire cette semaine

- Trancher le choix entre survol et clic pour ouvrir la fiche.
- Attribuer les lots A, B et C.
- Relire le contrat de messages à trois et le valider. Il a été mis à jour à partir du
  cahier des charges.
- Commiter les fichiers créés par Unity au premier lancement : les `.meta` des scripts du
  pont, le build profile `Web`, la scène de test et les réglages mis à jour. Les
  réglages vont dans une PR à part.
- Configurer UnityYAMLMerge sur chaque machine, avec la commande du README.
- Faire un build chacun, pour vérifier que tout le monde peut afficher la 3D dans la page.

## Lot A. Maquette, caméra et navigation

Côté Unity :

- Modéliser l'appartement avec ProBuilder : salon avec l'entrée, chambre, cuisine, murs
  à mi-hauteur, style maquette.
- Placer les 10 éléments, avec les compteurs près de l'entrée. Poser sur chacun un
  `InspectableElement` et un collider, avec les clés du JSON.
- Écrire le contrôle de caméra : rotation au clic droit, zoom à la molette.
- Bloquer la caméra au-dessus des murs, sans traverser les cloisons.
- Gérer les trois vues (appartement, pièce, élément) avec des transitions d'environ une
  seconde.
- Faire tourner lentement la maquette sur l'écran d'accueil.
- Recevoir `focusRoom` et `focusElement`, et envoyer `roomChanged`.
- Régler l'éclairage et les matériaux neutres.

Côté Vue :

- Ajouter les boutons Salon, Chambre et Cuisine, qui envoient `focusRoom`.
- Mettre à jour la pièce courante à la réception de `roomChanged`.

Livrables : les prefabs `Appartement` et `CameraRig`, le composant `RoomButtons.vue`.

## Lot B. Inspection et fiche

Côté Unity :

- Terminer `WebBridge`, avec la gestion d'erreurs et les logs.
- Illuminer l'élément survolé, dans une couleur réservée au survol.
- Détecter le clic sur un élément et envoyer `pointSelected`. Détecter le clic dans le
  vide et envoyer `deselected`.
- Mettre en surbrillance l'élément reçu par `select`.
- Jouer un son court à la validation d'un constat.
- Créer la scène principale `Main.unity` et son GameObject `WebBridge`.

Côté Vue :

- Terminer `useUnity`, avec la mise en attente des messages avant `ready`.
- Créer la fiche d'un élément : nom, pièce, choix de l'état, commentaire, bouton Valider.
  Échap ferme la fiche.
- Créer la fiche d'un compteur : saisie de l'index et calcul de la consommation entre
  l'entrée et la sortie.
- Brancher la fiche sur `EdlRepository.saveConstat`, qui enregistre déjà les constats
  dans le `localStorage`. Ajouter un bouton qui charge le jeu de démo avec `loadDemo`.
- Valider le contenu du JSON dans `JsonRepository`.

Livrables : la scène `Main.unity`, les composants `ElementCard.vue` et `MeterCard.vue`.

## Lot C. Repères, modes et comparaison

Côté Unity :

- Afficher un repère coloré sur chaque élément noté : vert pour Bon, orange pour
  Usage, rouge pour Mauvais. On peut cliquer sur le repère.
- Recevoir `setPoints` et mettre à jour les repères.
- En mode Comparer, faire passer progressivement au rouge les éléments dégradés.
- Afficher un effet visuel quand un repère apparaît.

Côté Vue :

- Créer l'écran d'accueil : nom du logement en haut, boutons Entrée, Sortie et
  Comparer. Entrée est mis en avant. Sortie et Comparer sont désactivés tant que l'entrée
  n'est pas faite.
- Après un clic sur Entrée, envoyer `focusRoom` vers le salon et afficher le message
  d'aide.
- Écrire `comparaison.ts`, qui trouve les éléments dégradés entre l'entrée et la sortie.
- Envoyer `setPoints` à chaque constat validé et à chaque changement de mode.
- En mode Comparer, afficher la liste des défauts. Un clic sur un défaut envoie
  `focusElement`.

Livrables : le prefab `PointMarker`, les composants `HomeScreen.vue`, `ModeSwitch.vue`
et `DefectList.vue`, le module `comparaison.ts`.

## Dépendances

Le contrat de messages bloque tout le reste. On le met à jour et on le valide à trois en
semaine 1. Ensuite :

- La maquette du lot A permet au lot B de tester le survol et le clic sur les vrais
  objets, et au lot C de placer les repères.
- Le pont du lot B et la scène `Main.unity` permettent aux lots A et C de tester dans la
  page.
- La fiche du lot B produit les constats dont le lot C a besoin pour les repères et la
  comparaison.

Pour ne pas s'attendre les uns les autres :

- Avant que la maquette existe, les lots B et C travaillent sur une scène de test avec
  des cubes qui portent un `InspectableElement` et les vraies clés du JSON.
- Le lot C peut tester ses repères et sa comparaison avec le jeu de démo, sans attendre
  la fiche du lot B.
- Côté Vue, une fausse version de `useUnity` écrit les messages envoyés dans la console
  et simule `ready` et `pointSelected`. On peut ainsi développer l'interface sans build
  WebGL.

## Jalons

Les jalons suivent l'ordre d'importance du cahier des charges. Les fonctionnalités 1 et
2 doivent marcher en premier.

| Semaine | Lot A | Lot B | Lot C | Objectif commun |
| ------- | ----- | ----- | ----- | --------------- |
| S1 | Plan de l'appartement avec les 10 éléments, premier contrôle de caméra | `Main.unity`, mise en attente des messages dans `useUnity` | Maquette de l'écran d'accueil et des couleurs d'état | Contrat validé, choix survol ou clic tranché, lots attribués |
| S2 | Les 3 pièces en volumes simples, murs à mi-hauteur, `InspectableElement` sur les 10 éléments | Survol, clic, `pointSelected`, fiche d'un élément | Repère coloré sur la scène de test, `setPoints` reçu | Fonctionnalités 1 et 2 sur la scène de test : survoler, cliquer, noter, voir le repère |
| S3 | Vues pièce et élément, transitions, caméra bloquée au-dessus des murs, boutons de pièces | Sauvegarde des constats, Échap et clic dans le vide | Écran d'accueil, modes Entrée et Sortie | Démo intermédiaire : une inspection d'entrée complète dans la vraie maquette |
| S4 | Rotation sur l'accueil, `focusElement` | Fiche compteur et consommation, son de validation | Mode Comparer, passage au rouge, liste des défauts | Fonctionnalités 1 à 6 en place, plus d'ajout après cette semaine |
| S5 | Éclairage, matériaux, taille du build | Erreurs et états de chargement | Effets d'apparition des repères, lisibilité | Chacun teste le lot d'un autre |
| S6 | Corrections | Corrections | Corrections | Rendu : build final, README, soutenance |

## Règles pour éviter les conflits dans Unity

Git fusionne mal les scènes et les prefabs Unity. On suit donc ces règles :

1. La scène principale `Assets/Scenes/Main.unity` a un seul propriétaire, le lot B. Les
   autres ne la modifient pas. Ils demandent au propriétaire d'y ajouter leur prefab,
   ou ouvrent une PR dédiée après l'avoir prévenu.
2. Chacun travaille dans ses propres prefabs et sa propre scène de test :
   - lot A dans `Assets/Lot-A/`, avec `Appartement`, `CameraRig` et `Lot-A_Sandbox.unity` ;
   - lot B dans `Assets/Lot-B/` et `Assets/Scripts/Bridge/` ;
   - lot C dans `Assets/Lot-C/`, avec `PointMarker`, les matériaux d'état et
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
- Une branche par tâche, nommée d'après le lot : `lot-a/camera`, `lot-b/fiche-element`,
  `lot-c/reperes`. On utilise aussi `contrat/...`, `fix/...` et `docs/...`.
- Les messages de commit sont en français, au présent, avec un préfixe : `feat`, `fix`,
  `docs`, `chore` ou `refactor`. Par exemple :
  `feat(lot-c): couleurs des repères par état`.
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
- [ ] La tâche respecte le cahier des charges : contrôles, couleurs d'état, transitions
      d'environ une seconde.
- [ ] La tâche respecte le contrat de messages. Si le contrat change,
      `docs/contrat-messages.md`, `bridge.ts` et `BridgeMessages.cs` changent dans la
      même PR.
- [ ] La Console Unity n'affiche aucune nouvelle erreur ni nouvel avertissement. Chaque
      fichier ajouté a son `.meta` commité. Le diff ne contient aucune modification de
      scène ou de réglage faite par erreur.
- [ ] `npm run lint`, `npm run type-check` et `npm run build` passent. Aucun accès aux
      données ne se fait hors d'un `EdlRepository`.
- [ ] On a testé sur Chrome et Firefox, sur ordinateur.
- [ ] Une autre personne a relu et approuvé la PR. Elle est mergée dans `main` et la
      branche est supprimée.
- [ ] Le README ou la documentation sont à jour si la tâche change l'installation ou
      l'usage.
