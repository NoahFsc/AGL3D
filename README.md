# AGL3D, état des lieux en 3D

Projet de cours du M2 MIAGE, en groupe de 3. L'application remplace la grille papier
d'un état des lieux par une maquette 3D d'appartement. L'utilisateur clique sur un
élément (mur, sol, évier, placard, compteur...), lui donne un état, et un repère coloré
apparaît à cet endroit. Il refait l'inspection à la sortie du locataire. En mode
Comparer, les éléments dégradés passent au rouge dans la 3D.

La partie 3D est un projet Unity 6 (URP) exporté en WebGL. Une application Vue l'affiche,
gère les fiches et enregistre les constats. Les deux échangent des messages JSON décrits
dans [docs/contrat-messages.md](docs/contrat-messages.md). Le
[cahier des charges](docs/cahier-des-charges.pdf) décrit le comportement attendu.

## Ce que doit faire la 3D

- Afficher la maquette de l'appartement, modélisée avec ProBuilder : un salon avec
  l'entrée et les compteurs, une chambre et une cuisine, avec des murs à mi-hauteur.
- Contenir 10 éléments à inspecter.
- Tourner autour de l'appartement au clic droit, zoomer à la molette, et déplacer la
  caméra vers une pièce ou un élément en environ une seconde. La caméra reste au-dessus
  des murs.
- Illuminer l'élément survolé et prévenir la page quand l'utilisateur clique dessus.
- Afficher un repère coloré sur chaque élément noté : vert pour Bon, orange pour Usage,
  rouge pour Mauvais.
- En mode Comparer, faire passer progressivement au rouge les éléments dégradés.

Chaque objet cliquable porte un composant `InspectableElement` avec une clé stable, par
exemple `salon.mur-ouest`. Les données de l'état des lieux utilisent la même clé.

## Organisation du dossier `unity/`

```
unity/
├── Assets/
│   ├── Plugins/WebGL/AGL3DBridge.jslib   Envoi des messages de Unity vers la page
│   └── Scripts/Bridge/
│       ├── WebBridge.cs                  Réception et envoi des messages
│       ├── BridgeMessages.cs             Format des messages
│       └── InspectableElement.cs         Clé d'un élément cliquable
├── Packages/
└── ProjectSettings/
```

Le reste du dépôt contient l'application Vue (`web/`) et la documentation (`docs/`).

## Prérequis

- Unity 6000.6.4f1 avec le module Web Build Support. Installez cette version exacte
  depuis Unity Hub.
- Git LFS, installé avec `brew install git-lfs`. Les images, modèles 3D et polices sont
  stockés en LFS.

Après le clone, récupérez les fichiers LFS :

```bash
git lfs install
```

```bash
git lfs pull
```

Configurez ensuite UnityYAMLMerge, qui fusionne les scènes et les prefabs lors d'un
conflit Git. Les commandes suivantes sont pour macOS :

```bash
git config merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge" merge -p %O %B %A %A'
```

```bash
git config merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
```

Sous Windows, le binaire se trouve dans
`C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\Tools\UnityYAMLMerge.exe`.

## Ouvrir le projet

Dans Unity Hub, choisissez Add, puis Add project from disk, et sélectionnez le dossier
`unity/`. N'ouvrez pas la racine du dépôt.

Au premier lancement, Unity crée les fichiers `.meta` des scripts du pont. Une seule
personne les commite, pour que tout le monde partage les mêmes GUID.

## Réglages Web à faire une fois

Ces réglages modifient `ProjectSettings/`. Prévenez l'équipe avant de les commiter.

1. Dans File > Build Profiles, sélectionnez Web puis Switch Platform.
2. Dans Player Settings > Web > Publishing Settings :
   - Compression Format sur Brotli ;
   - Decompression Fallback activé, car le serveur de dev Vite n'envoie pas les
     en-têtes de compression ;
   - Name Files As Hashes désactivé, pour garder des noms de fichiers fixes.
3. Dans la scène principale, ajoutez un GameObject nommé exactement `WebBridge` avec le
   composant `WebBridge`. La page envoie ses messages à ce nom.

Le script `WebBridge` laisse le clavier aux champs de la page
(`WebGLInput.captureAllKeyboardInput = false`). Aucun réglage n'est nécessaire dans
l'Éditeur pour ça.

## Build WebGL

Dans File > Build Profiles > Web, lancez Build et choisissez le dossier
`web/public/unity/`. Unity nomme les fichiers d'après ce dossier, par exemple
`Build/unity.loader.js`. Git ignore ce dossier, donc chacun fait son build en local.

Pour voir le résultat dans la page, lancez l'application Vue :

```bash
cd web && nvm use && npm install && npm run dev
```

Ouvrez ensuite http://localhost:5173.

## Documentation

- [Cahier des charges](docs/cahier-des-charges.pdf)
- [Contrat de messages entre Vue et Unity](docs/contrat-messages.md)
- [Plan d'action de l'équipe](docs/PLAN.md), avec la répartition des lots et les règles
  pour éviter les conflits dans Unity
