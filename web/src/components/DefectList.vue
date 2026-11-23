<script setup lang="ts">
/**
 * Liste des défauts du mode Comparer (C7) : les éléments dégradés entre l'entrée et la
 * sortie, du plus dégradé au moins dégradé, avec leur pièce, « avant → après » et le
 * commentaire de sortie. Un clic sur un défaut émet `ouvrir` : la page envoie
 * `focusElement` pour amener la caméra dessus et ouvre sa fiche de comparaison.
 */
import { computed } from 'vue'
import type { Edl, Element, ElementKey, Piece } from '@/types/edl'
import { trouverDegradations } from '@/utils/comparaison'
import { BADGES_ETAT } from '@/utils/etats'

const props = defineProps<{
  pieces: Piece[]
  elements: Element[]
  entree: Edl | null
  sortie: Edl | null
  /** Élément dont la fiche est ouverte. */
  selection: ElementKey | null
}>()

const emit = defineEmits<{ ouvrir: [key: ElementKey] }>()

const defauts = computed(() =>
  trouverDegradations(props.entree, props.sortie).map((degradation) => {
    const element = props.elements.find((e) => e.key === degradation.elementKey)
    const piece = props.pieces.find((p) => p.key === element?.pieceKey)
    return {
      ...degradation,
      label: element?.label ?? degradation.elementKey,
      piece: piece?.label ?? '',
    }
  }),
)
</script>

<template>
  <section
    class="flex flex-col gap-3 rounded-lg border border-neutrals-50 bg-white p-4"
    aria-labelledby="titre-defauts"
  >
    <header class="flex items-baseline justify-between gap-2">
      <h2 id="titre-defauts" class="text-h3 font-semibold">Défauts</h2>
      <span
        v-if="defauts.length > 0"
        class="rounded-full bg-mauvais-100 px-2 py-0.5 text-caption font-semibold text-mauvais-500"
      >
        {{ defauts.length }}
      </span>
    </header>

    <p v-if="defauts.length === 0" class="text-neutrals-200">
      Aucun défaut&nbsp;: tous les éléments sont dans le même état qu'à l'entrée, ou meilleur.
    </p>

    <ol v-else class="flex flex-col gap-2">
      <li v-for="defaut in defauts" :key="defaut.elementKey">
        <button
          type="button"
          class="flex w-full cursor-pointer flex-col gap-1.5 rounded-md border px-3 py-2.5 text-left hover:bg-neutrals-50/40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300"
          :class="
            selection === defaut.elementKey
              ? 'border-primary-300 bg-neutrals-50/40'
              : 'border-neutrals-50'
          "
          :aria-current="selection === defaut.elementKey ? 'true' : undefined"
          @click="emit('ouvrir', defaut.elementKey)"
        >
          <span class="flex items-start justify-between gap-3">
            <span class="min-w-0">
              <span class="block font-semibold">{{ defaut.label }}</span>
              <span class="block text-caption text-neutrals-200">{{ defaut.piece }}</span>
            </span>
            <span
              class="flex shrink-0 items-center gap-1"
              :aria-label="`${BADGES_ETAT[defaut.avant].label} à l'entrée, ${BADGES_ETAT[defaut.apres].label} à la sortie`"
            >
              <span
                class="rounded-full px-2 py-0.5 text-caption font-semibold opacity-70"
                :class="BADGES_ETAT[defaut.avant].classes"
              >
                {{ BADGES_ETAT[defaut.avant].label }}
              </span>
              <!-- Lucide « arrow-right » -->
              <svg
                class="text-neutrals-200"
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true"
              >
                <path d="M5 12h14" />
                <path d="m12 5 7 7-7 7" />
              </svg>
              <span
                class="rounded-full px-2 py-0.5 text-caption font-semibold"
                :class="BADGES_ETAT[defaut.apres].classes"
              >
                {{ BADGES_ETAT[defaut.apres].label }}
              </span>
            </span>
          </span>
          <span
            v-if="defaut.constatSortie.commentaire"
            class="line-clamp-2 text-caption text-neutrals-300"
          >
            {{ defaut.constatSortie.commentaire }}
          </span>
        </button>
      </li>
    </ol>
  </section>
</template>
