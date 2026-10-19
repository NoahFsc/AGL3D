<script setup lang="ts">
/**
 * Écran d'accueil posé par-dessus la 3D : boutons Entrée, Sortie et Comparer.
 *
 * - En mode `accueil`, Entrée est mis en avant ; ensuite, le mode en cours l'est.
 * - Sortie et Comparer restent désactivés tant que `entreeFaite` est faux.
 * - Seuls les boutons captent la souris : le reste laisse passer les clics et le
 *   glisser vers le canvas Unity.
 * - Émet `choisir` avec le mode cliqué.
 */
import type { ModeAffichage } from '@/types/bridge'

/** Modes qu'on choisit depuis l'écran d'accueil. */
export type ModeChoisi = Exclude<ModeAffichage, 'accueil'>

defineProps<{
  mode: ModeAffichage
  entreeFaite: boolean
}>()

const emit = defineEmits<{ choisir: [mode: ModeChoisi] }>()

const boutons: { mode: ModeChoisi; label: string; apresEntree: boolean }[] = [
  { mode: 'entree', label: 'Entrée', apresEntree: false },
  { mode: 'sortie', label: 'Sortie', apresEntree: true },
  { mode: 'comparer', label: 'Comparer', apresEntree: true },
]
</script>

<template>
  <div class="home">
    <nav class="home__modes" aria-label="Choix de l'état des lieux">
      <button
        v-for="bouton in boutons"
        :key="bouton.mode"
        type="button"
        class="agl-bouton home__bouton"
        :class="{
          'agl-bouton--fill':
            mode === bouton.mode || (mode === 'accueil' && bouton.mode === 'entree'),
        }"
        :aria-pressed="mode === bouton.mode"
        :disabled="bouton.apresEntree && !entreeFaite"
        :title="
          bouton.apresEntree && !entreeFaite ? 'Faites d\'abord l\'état des lieux d\'entrée' : ''
        "
        @click="emit('choisir', bouton.mode)"
      >
        {{ bouton.label }}
      </button>
    </nav>
  </div>
</template>

<style scoped>
.home {
  position: absolute;
  inset: 0;
  padding: 16px;
  pointer-events: none;
}

.home__modes {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  pointer-events: auto;
}

.home__bouton {
  box-shadow: var(--elevation-raised);
}

@media (width < 768px) {
  .home {
    padding: 12px;
  }

  .home__modes {
    gap: 8px;
  }

  .home__bouton {
    flex: 1;
    height: 48px;
    padding: 0 12px;
  }
}
</style>
