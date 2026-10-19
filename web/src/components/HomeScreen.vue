<script setup lang="ts">
import type { ModeAffichage } from '@/types/bridge'

/** Modes qu'on choisit depuis l'écran d'accueil. */
export type ModeChoisi = Exclude<ModeAffichage, 'accueil'>

defineProps<{
  /** Mode courant : en `accueil`, Entrée est mis en avant. */
  mode: ModeAffichage
  /** Sortie et Comparer restent désactivés tant que l'entrée n'est pas faite. */
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
  <!-- Posé par-dessus la 3D : seuls les boutons captent la souris, le reste laisse
       passer les clics et le glisser vers le canvas Unity. -->
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

/* Elevation/Raised : les boutons se détachent de la 3D. */
.home__bouton {
  box-shadow: var(--elevation-raised);
}

/* Mobile : bouton Size=Large (48 px), les trois se partagent la largeur. */
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
