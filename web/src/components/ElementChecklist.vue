<script setup lang="ts">
/**
 * Liste des éléments de l'état des lieux en cours, rangés par pièce : ce qui est noté,
 * avec son état, et ce qui reste à noter. Un clic sur une ligne émet `ouvrir` pour
 * ouvrir la fiche de l'élément.
 */
import { computed } from 'vue'
import type { Constat, Element, ElementKey, Etat, Piece, TypeEdl } from '@/types/edl'

const props = defineProps<{
  type: TypeEdl
  pieces: Piece[]
  elements: Element[]
  constats: Constat[]
  /** Élément dont la fiche est ouverte. */
  selection: ElementKey | null
}>()

const emit = defineEmits<{ ouvrir: [key: ElementKey] }>()

const ETATS: Record<Etat, { label: string; classes: string }> = {
  bon: { label: 'Bon', classes: 'bg-bon-100 text-bon-500' },
  usage: { label: 'Usage', classes: 'bg-usage-100 text-usage-500' },
  mauvais: { label: 'Mauvais', classes: 'bg-mauvais-100 text-mauvais-500' },
}

const parElement = computed(() => new Map(props.constats.map((c) => [c.elementKey, c])))

const groupes = computed(() =>
  props.pieces
    .map((piece) => {
      const lignes = props.elements
        .filter((e) => e.pieceKey === piece.key)
        .map((element) => ({ element, constat: parElement.value.get(element.key) ?? null }))
      return { piece, lignes, notes: lignes.filter((l) => l.constat).length }
    })
    .filter((g) => g.lignes.length > 0),
)

const total = computed(() => props.elements.length)
const notes = computed(() => props.elements.filter((e) => parElement.value.has(e.key)).length)
const complet = computed(() => total.value > 0 && notes.value === total.value)

function formaterIndex(constat: Constat): string {
  const releve = constat.releve
  if (!releve) return ''
  return `${releve.valeur.toLocaleString('fr-FR', { maximumFractionDigits: 3 })} ${releve.unite}`
}
</script>

<template>
  <section
    class="flex flex-col gap-4 rounded-lg border border-neutrals-50 bg-white p-4"
    :aria-label="`Éléments de l'état des lieux de ${type === 'entree' ? 'entrée' : 'sortie'}`"
  >
    <header class="flex flex-col gap-2">
      <h2 class="text-h3 font-semibold">
        {{ type === 'entree' ? 'Entrée' : 'Sortie' }}&nbsp;: éléments
      </h2>
      <p v-if="complet" class="flex items-center gap-2 font-semibold text-bon-500">
        <!-- Lucide « circle-check » -->
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <circle cx="12" cy="12" r="10" />
          <path d="m9 12 2 2 4-4" />
        </svg>
        Tous les éléments sont notés
      </p>
      <p v-else class="text-neutrals-200">
        <strong class="font-semibold text-neutrals-400">{{ notes }} sur {{ total }}</strong>
        notés, {{ total - notes }} à noter
      </p>
      <div
        class="h-2 overflow-hidden rounded-full bg-neutrals-50"
        role="progressbar"
        :aria-valuenow="notes"
        aria-valuemin="0"
        :aria-valuemax="total"
        aria-label="Éléments notés"
      >
        <div
          class="h-full rounded-full transition-[width] duration-300"
          :class="complet ? 'bg-bon-300' : 'bg-primary-300'"
          :style="{ width: `${total ? (notes / total) * 100 : 0}%` }"
        />
      </div>
    </header>

    <div v-for="groupe in groupes" :key="groupe.piece.key" class="flex flex-col gap-1">
      <h3 class="flex items-baseline justify-between px-2 font-semibold text-neutrals-300">
        {{ groupe.piece.label }}
        <span class="text-caption font-normal text-neutrals-200">
          {{ groupe.notes }}/{{ groupe.lignes.length }}
        </span>
      </h3>
      <ul class="flex flex-col">
        <li v-for="{ element, constat } in groupe.lignes" :key="element.key">
          <button
            type="button"
            class="flex min-h-11 w-full cursor-pointer items-center gap-3 rounded-sm px-2 py-2 text-left hover:bg-neutrals-50/40 focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-accent-300"
            :class="selection === element.key ? 'bg-neutrals-50/60 font-semibold' : ''"
            :aria-current="selection === element.key ? 'true' : undefined"
            @click="emit('ouvrir', element.key)"
          >
            <!-- Lucide « circle-check » (noté) ou « circle-dashed » (à noter) -->
            <svg
              class="shrink-0"
              :class="constat ? 'text-primary-300' : 'text-neutrals-200'"
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              aria-hidden="true"
            >
              <template v-if="constat">
                <circle cx="12" cy="12" r="10" />
                <path d="m9 12 2 2 4-4" />
              </template>
              <template v-else>
                <path d="M10.1 2.182a10 10 0 0 1 3.8 0" />
                <path d="M13.9 21.818a10 10 0 0 1-3.8 0" />
                <path d="M17.609 3.721a10 10 0 0 1 2.69 2.7" />
                <path d="M2.182 13.9a10 10 0 0 1 0-3.8" />
                <path d="M20.279 17.609a10 10 0 0 1-2.7 2.69" />
                <path d="M21.818 10.1a10 10 0 0 1 0 3.8" />
                <path d="M3.721 6.391a10 10 0 0 1 2.7-2.69" />
                <path d="M6.391 20.279a10 10 0 0 1-2.69-2.7" />
              </template>
            </svg>
            <span class="min-w-0 flex-1">
              <span class="block truncate">{{ element.label }}</span>
              <span v-if="constat?.releve" class="block text-caption text-neutrals-200">
                Index {{ formaterIndex(constat) }}
              </span>
            </span>
            <span
              v-if="constat"
              class="shrink-0 rounded-full px-2 py-0.5 text-caption font-semibold"
              :class="ETATS[constat.etat].classes"
            >
              {{ ETATS[constat.etat].label }}
            </span>
            <span v-else class="shrink-0 text-caption text-neutrals-200">À noter</span>
          </button>
        </li>
      </ul>
    </div>
  </section>
</template>
