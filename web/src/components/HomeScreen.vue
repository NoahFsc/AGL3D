<script setup lang="ts">
/**
 * Boutons Entrée, Sortie et Comparer posés sur la 3D. Sortie et Comparer attendent
 * que l'entrée soit faite. Émet `choisir` avec le mode cliqué.
 */
import type { ModeAffichage } from '@/types/bridge'

export type ModeChoisi = Exclude<ModeAffichage, 'accueil'>

const props = defineProps<{
  mode: ModeAffichage
  entreeFaite: boolean
}>()

const emit = defineEmits<{ choisir: [mode: ModeChoisi] }>()

const boutons: { mode: ModeChoisi; label: string; apresEntree: boolean }[] = [
  { mode: 'entree', label: 'Entrée', apresEntree: false },
  { mode: 'sortie', label: 'Sortie', apresEntree: true },
  { mode: 'comparer', label: 'Comparer', apresEntree: true },
]

/** Bouton Fill pour Entrée sur l'accueil et pour le mode en cours, Outline sinon. */
function classeBouton(bouton: (typeof boutons)[number]): string {
  if (bouton.apresEntree && !props.entreeFaite) {
    return 'cursor-not-allowed border-neutrals-50 bg-white text-neutrals-100'
  }
  const enAvant =
    props.mode === bouton.mode || (props.mode === 'accueil' && bouton.mode === 'entree')
  return enAvant
    ? 'cursor-pointer border-primary-300 bg-primary-300 text-white'
    : 'cursor-pointer border-primary-300 bg-white text-primary-300'
}
</script>

<template>
  <div class="pointer-events-none absolute inset-0 p-4 max-md:p-3">
    <nav
      class="pointer-events-auto flex flex-wrap gap-3 max-md:gap-2"
      aria-label="Choix de l'état des lieux"
    >
      <button
        v-for="bouton in boutons"
        :key="bouton.mode"
        type="button"
        class="inline-flex h-9 items-center justify-center rounded-sm border-2 px-4 whitespace-nowrap shadow-raised focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300 max-md:h-12 max-md:flex-1 max-md:px-3"
        :class="classeBouton(bouton)"
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
