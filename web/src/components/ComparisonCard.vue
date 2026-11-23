<script setup lang="ts">
/**
 * Fiche d'un élément en mode Comparer, en lecture seule : l'état et le commentaire à
 * l'entrée puis à la sortie, et l'évolution entre les deux. Pour un compteur, les deux
 * index et la consommation. Comme `ElementCard`, elle se cale sur le bord opposé au point
 * cliqué ; Échap ou la croix la ferment.
 */
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { Constat, Edl, Element } from '@/types/edl'
import { comparerElement, type Evolution } from '@/utils/comparaison'
import { BADGES_ETAT, formaterNombre } from '@/utils/etats'

const props = defineProps<{
  element: Element
  piece: string
  entree: Edl | null
  sortie: Edl | null
  /** Abscisse du point cliqué, de 0 à 1 dans le canvas. */
  x: number
}>()

const emit = defineEmits<{ fermer: [] }>()

const comparaison = computed(() => comparerElement(props.entree, props.sortie, props.element.key))

const EVOLUTIONS: Record<Evolution, { texte: string; classes: string }> = {
  degrade: { texte: 'Dégradé depuis l’entrée', classes: 'bg-mauvais-100 text-mauvais-500' },
  ameliore: { texte: 'En meilleur état qu’à l’entrée', classes: 'bg-bon-100 text-bon-500' },
  identique: { texte: 'Même état qu’à l’entrée', classes: 'bg-neutrals-50/40 text-neutrals-300' },
  incomplet: {
    texte: 'Pas noté à l’entrée et à la sortie : rien à comparer',
    classes: 'bg-neutrals-50/40 text-neutrals-300',
  },
}

const colonnes = computed(() => [
  { titre: 'Entrée', date: props.entree?.date, constat: comparaison.value.avant },
  { titre: 'Sortie', date: props.sortie?.date, constat: comparaison.value.apres },
])

function formaterDate(iso: string | undefined): string {
  if (!iso) return ''
  return new Date(`${iso}T00:00:00`).toLocaleDateString('fr-FR', { dateStyle: 'long' })
}

function formaterIndex(constat: Constat): string {
  return constat.releve ? `${formaterNombre(constat.releve.valeur)} ${constat.releve.unite}` : ''
}

/** Point cliqué à droite : la fiche passe à gauche. */
const aGauche = computed(() => props.x > 0.55)

const titre = ref<HTMLElement | null>(null)
watch(
  () => props.element.key,
  async () => {
    await nextTick()
    titre.value?.focus()
  },
  { immediate: true },
)

function fermerAvecEchap(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('fermer')
}

onMounted(() => document.addEventListener('keydown', fermerAvecEchap))
onBeforeUnmount(() => document.removeEventListener('keydown', fermerAvecEchap))
</script>

<template>
  <aside
    class="absolute top-16 bottom-16 flex w-80 flex-col overflow-hidden rounded-md border border-neutrals-50 bg-white shadow-raised max-md:inset-x-3 max-md:top-auto max-md:bottom-3 max-md:max-h-[calc(100%-1.5rem)] max-md:w-auto"
    :class="aGauche ? 'left-4' : 'right-4'"
    role="dialog"
    :aria-labelledby="`comparaison-${element.key}`"
  >
    <header class="flex items-start gap-3 px-5 pt-5 pb-3">
      <div class="min-w-0 flex-1">
        <h2
          :id="`comparaison-${element.key}`"
          ref="titre"
          tabindex="-1"
          class="text-h3 font-semibold outline-none"
        >
          {{ element.label }}
        </h2>
        <p class="text-neutrals-200">{{ piece }}</p>
      </div>
      <button
        type="button"
        class="-mt-1 -mr-2 inline-flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-sm text-neutrals-300 hover:bg-neutrals-50/40 focus-visible:outline-2 focus-visible:outline-accent-300"
        aria-label="Fermer la fiche"
        title="Fermer (Échap)"
        @click="emit('fermer')"
      >
        <!-- Lucide « x » -->
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
          <path d="M18 6 6 18" />
          <path d="m6 6 12 12" />
        </svg>
      </button>
    </header>

    <div class="flex flex-1 flex-col gap-4 overflow-y-auto px-5 pb-5">
      <p
        class="rounded-sm px-3 py-2 font-semibold"
        :class="EVOLUTIONS[comparaison.evolution].classes"
      >
        {{ EVOLUTIONS[comparaison.evolution].texte }}
      </p>

      <section v-for="colonne in colonnes" :key="colonne.titre" class="flex flex-col gap-2">
        <h3 class="flex items-baseline justify-between font-semibold">
          {{ colonne.titre }}
          <span class="text-caption font-normal text-neutrals-200">
            {{ formaterDate(colonne.date) }}
          </span>
        </h3>
        <template v-if="colonne.constat">
          <span
            class="self-start rounded-full px-2 py-0.5 text-caption font-semibold"
            :class="BADGES_ETAT[colonne.constat.etat].classes"
          >
            {{ BADGES_ETAT[colonne.constat.etat].label }}
          </span>
          <p v-if="colonne.constat.releve" class="text-neutrals-300">
            Index {{ formaterIndex(colonne.constat) }}
          </p>
          <p v-if="colonne.constat.commentaire" class="whitespace-pre-line text-neutrals-300">
            {{ colonne.constat.commentaire }}
          </p>
          <p v-else class="text-caption text-neutrals-200">Pas de commentaire</p>
        </template>
        <p v-else class="text-neutrals-200">Pas noté</p>
      </section>

      <p
        v-if="comparaison.consommation !== null"
        class="rounded-sm px-3 py-2"
        :class="
          comparaison.consommation < 0 ? 'bg-mauvais-100 text-mauvais-500' : 'bg-neutrals-50/40'
        "
      >
        <template v-if="comparaison.consommation < 0">
          L'index de sortie est plus petit que celui de l'entrée.
        </template>
        <template v-else>
          Consommation&nbsp;:
          <strong class="font-semibold">
            {{ formaterNombre(comparaison.consommation) }}&nbsp;{{
              comparaison.apres?.releve?.unite
            }}
          </strong>
        </template>
      </p>
    </div>
  </aside>
</template>
