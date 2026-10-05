# AGL3D, application web

Application Vue 3 en TypeScript, construite avec Vite. Elle affiche le build Unity WebGL
et les fiches de l'état des lieux. L'installation est décrite dans le
[README racine](../README.md), et les messages échangés avec Unity dans
[docs/contrat-messages.md](../docs/contrat-messages.md).

| Commande             | Effet                                                    |
| -------------------- | -------------------------------------------------------- |
| `npm run dev`        | Lance le serveur de dev sur http://localhost:5173        |
| `npm run build`      | Vérifie les types, puis construit la version de production |
| `npm run lint`       | Lance ESLint et corrige ce qu'il peut                    |
| `npm run format`     | Formate `src/` avec Prettier                             |
| `npm run type-check` | Vérifie les types avec `vue-tsc`                         |
