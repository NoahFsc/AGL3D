# Plan d'action

Ce plan suit le [cahier des charges](cahier-des-charges.pdf). Nous sommes trois :
Antoine, Noah et Sara. On découpe le projet en trois lots, un par personne. Chaque lot
contient une partie Unity et une partie Vue, pour que chacun de nous travaille sur les
deux.

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

## Qui fait quoi

| Lot | Qui | Sujet |
| --- | --- | ----- |
| A | Antoine | L'appartement et la caméra |
| B | Noah | Cliquer sur un élément et le noter |
| C | Sara | L'accueil, les couleurs et la comparaison |

Chacun de nous travaille à la fois dans Unity et dans Vue.

## Où on en est

### Ce qui marche

La connexion entre Unity et Vue est faite. La page Vue charge le build Unity WebGL, et
les deux côtés s'échangent des messages. Le build contient une seule scène, `Main.unity`,
avec l'appartement et ses 10 éléments. Au démarrage, Unity envoie `ready` avec leurs clés
et la page l'affiche. Les messages que Vue envoie avant `ready` sont mis en attente.

Le dépôt contient déjà :

- le projet Unity dans `unity/` et l'application Vue dans `web/` ;
- les scripts du pont : `WebBridge`, `InspectableElement`, `BridgeMessages` et le
  fichier `AGL3DBridge.jslib` ;
- le build profile `Web`, avec une sortie dans `web/public/unity/` ;
- côté Vue, le composable `useUnity`, le composant `UnityViewer.vue` et une page de test
  qui liste les messages reçus d'Unity ;
- les types TypeScript, avec les états `bon`, `usage` et `mauvais` ;
- `EdlRepository` et `JsonRepository`, qui lisent les données de référence et
  enregistrent les constats dans le `localStorage` ;
- le fichier de démo `appart-demo.json`, avec les 10 éléments et un jeu de démo
  d'entrée et de sortie ;
- le contrat de messages, le README et ce plan.

### Ce qui manque

- Aucun script Unity ne réagit encore aux messages reçus. `WebBridge` les reçoit, puis
  affiche un avertissement "Aucun abonné".
- La caméra est fixe.
- Le fichier de démo ne référence aucune photo. Le champ `photo` existe dans les
  constats, mais vaut `null`.

## Avant de commencer

- Tous les trois : on relit et on valide le contrat de messages, et on tranche le choix
  entre survol et clic pour ouvrir la fiche.
- Tous les trois : on configure UnityYAMLMerge et Git LFS, puis chacun fait un build et
  vérifie qu'il voit la 3D dans la page.
- Antoine : crée une **scène de test avec 10 cubes** qui portent les vraies clés du JSON.
  Noah et Sara s'en servent en attendant l'appartement. C'est la première tâche à faire,
  parce qu'elle débloque les autres.

## Les tâches

Une tâche correspond à une branche et à une PR. ✓ veut dire qu'on peut la commencer tout
de suite. Sinon, la colonne « Attend » indique la tâche qui doit être finie avant.

### Antoine : l'appartement et la caméra

| # | Côté | Tâche | Attend |
| - | ---- | ----- | ------ |
| A1 | Unity | Construire les 3 pièces avec ProBuilder : salon avec l'entrée, chambre, cuisine, murs à mi-hauteur, style maquette. | ✓ |
| A2 | Unity | Placer les 10 éléments, avec les compteurs près de l'entrée. Poser sur chacun un collider et un `InspectableElement` avec les clés du JSON. Prefab `Appartement`. | A1 |
| A3 | Unity | Faire tourner la caméra au clic droit et zoomer à la molette. Prefab `CameraRig`. | ✓ |
| A4 | Unity | Garder la caméra au-dessus des murs, sans traverser les cloisons. | A1, A3 |
| A5 | Unity | Déplacer la caméra vers une pièce en environ une seconde : recevoir `focusRoom`, envoyer `roomChanged`. | A3 |
| A6 | Vue | `RoomButtons.vue` : boutons Salon, Chambre et Cuisine, pièce courante mise à jour par `roomChanged`. | A5 |
| A7 | Unity | Rapprocher la caméra d'un élément : au clic, et à la réception de `focusElement`. | A5 |
| A8 | Unity | Régler l'éclairage doux et les matériaux neutres, surveiller la taille du build. | A2 |

### Noah : cliquer sur un élément et le noter

| # | Côté | Tâche | Attend |
| - | ---- | ----- | ------ |
| B1 | Unity + Vue | Créer `Main.unity` avec son GameObject `WebBridge`. Dans `useUnity`, mettre en attente les messages envoyés avant `ready`. | ✓ |
| B2 | Unity | Illuminer l'élément survolé, dans une couleur réservée au survol. | cubes de test |
| B3 | Unity | Détecter le clic sur un élément et envoyer `pointSelected`. Détecter le clic dans le vide et envoyer `deselected`. Recevoir `select`. | B2 |
| B4 | Vue | `ElementCard.vue` : nom, pièce, choix de l'état, commentaire, bouton Valider. Échap ferme la fiche. | B3 |
| B5 | Vue | Enregistrer les constats avec `EdlRepository.saveConstat`. Ajouter un bouton qui charge le jeu de démo avec `loadDemo`. Valider le JSON dans `JsonRepository`. | B4 |
| B6 | Vue | `MeterCard.vue` : saisie de l'index et calcul de la consommation entre l'entrée et la sortie. | B4 |
| B7 | Unity | Jouer un son court à la validation d'un constat. | B4 |
| B8 | Vue | Afficher un écran de chargement, et une erreur si le build ne se charge pas. | B1 |

### Sara : l'accueil, les couleurs et la comparaison

| # | Côté | Tâche | Attend |
| - | ---- | ----- | ------ |
| C1 | Vue | `HomeScreen.vue` : nom du logement en haut, boutons Entrée, Sortie et Comparer par-dessus la 3D. Entrée est mis en avant. Sortie et Comparer restent désactivés tant que l'entrée n'est pas faite. | ✓ |
| C2 | Unity | Faire tourner lentement la maquette en mode `accueil` (message `init`). | ✓ |
| C3 | Unity | Prefab `PointMarker` : un repère vert, orange ou rouge sur chaque élément noté. Recevoir `setPoints`. On peut cliquer sur le repère. | cubes de test |
| C4 | Vue | `ModeSwitch.vue` : passer d'un mode à l'autre, envoyer `init` puis `setPoints`. Après un clic sur Entrée, envoyer `focusRoom` vers le salon et afficher le message d'aide. | C1 |
| C5 | Vue | `comparaison.ts` : trouver les éléments dégradés entre l'entrée et la sortie. | ✓ |
| C6 | Unity | En mode Comparer, faire passer progressivement au rouge les éléments dégradés. | C3, C5 |
| C7 | Vue | `DefectList.vue` : liste des défauts. Un clic sur un défaut envoie `focusElement`. | C4, A7 |
| C8 | Unity | Ajouter un effet visuel quand un repère apparaît. | C3 |

## Qui attend qui

- **Les cubes de test d'Antoine** débloquent B2 pour Noah et C3 pour Sara. Antoine les
  fait en premier.
- **Les constats enregistrés par Noah (B5)** servent aux modes de Sara (C4). En
  attendant, Sara utilise le jeu de démo.
- **Le rapprochement de la caméra d'Antoine (A7)** est nécessaire à la liste des
  défauts de Sara (C7).
- **La rotation de l'accueil (C2)** fait tourner la maquette, pas la caméra. Sara et
  Antoine se mettent d'accord pour que C2 ne touche pas au `CameraRig`.
- L'appartement d'Antoine (A2) ne bloque personne : Noah et Sara développent sur les
  cubes de test, puis passent à l'appartement quand il est prêt.

## Les étapes

On passe à l'étape suivante quand la précédente marche dans le build WebGL affiché par
la page.

1. **Noter un élément** (fonctionnalités 1 et 2). Sur les cubes de test : survoler,
   cliquer, noter, voir le repère coloré. Tâches B2, B3, B4 et C3.
2. **Faire une entrée complète** dans le vrai appartement : accueil, Entrée, changer de
   pièce, noter, enregistrer. Tâches A2 à A6, B5, C1, C2 et C4.
3. **Comparer l'entrée et la sortie** (fonctionnalités 3 à 6). Tâches A7, B6, C5, C6 et
   C7. Après cette étape, on n'ajoute plus de fonctionnalité.
4. **Finir.** Tâches A8, B7, B8 et C8. Chacun teste le travail d'un autre : Antoine teste
   celui de Noah, Noah celui de Sara, Sara celui d'Antoine.
5. **Rendre.** On corrige, on fait le build final, on met le README à jour et on prépare
   la soutenance.

Si le temps manque, on retire dans cet ordre : la liste des défauts (C7) et le
rapprochement de la caméra (A7), puis la fiche des compteurs (B6), puis les effets et le
son (C8, B7).

## Règles pour éviter les conflits dans Unity

Git fusionne mal les scènes et les prefabs Unity. On suit donc ces règles :

1. La scène principale `Assets/Scenes/Main.unity` a un seul propriétaire : Noah.
   Antoine et Sara ne la modifient pas. Ils demandent à Noah d'y ajouter leur prefab, ou
   ouvrent une PR dédiée après l'avoir prévenu.
2. Chacun travaille dans ses propres prefabs et sa propre scène de test :
   - Antoine dans `Assets/Lot-A/`, avec `Appartement`, `CameraRig`, `Lot-A_Sandbox.unity`
     et la scène de test avec les 10 cubes ;
   - Noah dans `Assets/Lot-B/` et `Assets/Scripts/Bridge/` ;
   - Sara dans `Assets/Lot-C/`, avec `PointMarker`, les matériaux d'état, la rotation de
     l'accueil et `Lot-C_Sandbox.unity`.

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
- Une branche par tâche, nommée d'après le lot : `lot-a/camera` pour Antoine,
  `lot-b/fiche-element` pour Noah, `lot-c/reperes` pour Sara. On utilise aussi
  `contrat/...`, `fix/...` et `docs/...`.
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
