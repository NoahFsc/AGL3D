<script setup lang="ts">
/**
 * Liste du mode Comparer, rangée par pièce. Un élément dont l'état a changé montre
 * « avant → après » ; un élément inchangé montre son état une seule fois. Pour un
 * compteur, la ligne donne la consommation entre les deux relevés. Un clic sur une ligne
 * émet `ouvrir` pour ouvrir la fiche de comparaison de l'élément.
 */
import { computed } from 'vue'
import type { Edl, Element, ElementKey, Piece } from '@/types/edl'
import { comparerElement } from '@/utils/comparaison'
import { BADGES_ETAT, formaterNombre } from '@/utils/etats'

const props = defineProps<{
  pieces: Piece[]
  elements: Element[]
  entree: Edl | null
  sortie: Edl | null
  /** Élément dont la fiche est ouverte. */
  selection: ElementKey | null
}>()

const emit = defineEmits<{ ouvrir: [key: ElementKey] }>()

const lignes = computed(() =>
  props.elements.map((element) => ({
    element,
    ...comparerElement(props.entree, props.sortie, element.key),
  })),
)

const groupes = computed(() =>
  props.pieces
    .map((piece) => ({
      piece,
      lignes: lignes.value.filter((l) => l.element.pieceKey === piece.key),
    }))
    .filter((g) => g.lignes.length > 0),
)

const nbDegrades = computed(() => lignes.value.filter((l) => l.evolution === 'degrade').length)
const nbAmeliores = computed(() => lignes.value.filter((l) => l.evolution === 'ameliore').length)
</script>

<template>
  <section
    class="flex flex-col gap-4 rounded-lg border border-neutrals-50 bg-white p-4"
    aria-label="Comparaison de l'entrée et de la sortie"
  >
    <header class="flex flex-col gap-1">
      <h2 class="text-h3 font-semibold">Comparer&nbsp;: entrée et sortie</h2>
      <p v-if="nbDegrades > 0" class="font-semibold text-mauvais-500">
        {{ nbDegrades }} élément{{ nbDegrades > 1 ? 's' : '' }} dégradé{{ nbDegrades > 1 ? 's' : '' }}
      </p>
      <p v-else class="font-semibold text-bon-500">Aucun élément dégradé</p>
      <p v-if="nbAmeliores > 0" class="text-caption text-neutrals-200">
        {{ nbAmeliores }} élément{{ nbAmeliores > 1 ? 's' : '' }} en meilleur état qu'à l'entrée
      </p>
    </header>

    <div v-for="groupe in groupes" :key="groupe.piece.key" class="flex flex-col gap-1">
      <h3 class="px-2 font-semibold text-neutrals-300">{{ groupe.piece.label }}</h3>
      <ul class="flex flex-col">
        <li v-for="ligne in groupe.lignes" :key="ligne.element.key">
          <button
            type="button"
            class="flex min-h-11 w-full cursor-pointer items-center gap-3 rounded-sm px-2 py-2 text-left hover:bg-neutrals-50/40 focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-accent-300"
            :class="selection === ligne.element.key ? 'bg-neutrals-50/60 font-semibold' : ''"
            :aria-current="selection === ligne.element.key ? 'true' : undefined"
            @click="emit('ouvrir', ligne.element.key)"
          >
            <span class="min-w-0 flex-1">
              <span class="block truncate">{{ ligne.element.label }}</span>
              <span
                v-if="ligne.consommation !== null"
                class="block text-caption"
                :class="ligne.consommation < 0 ? 'text-mauvais-500' : 'text-neutrals-200'"
              >
                <template v-if="ligne.consommation < 0">Index de sortie plus petit qu'à l'entrée</template>
                <template v-else>
                  {{ formaterNombre(ligne.consommation) }}&nbsp;{{ ligne.apres?.releve?.unite }} consommés
                </template>
              </span>
              <span
                v-else-if="ligne.evolution === 'incomplet' && (ligne.avant || ligne.apres)"
                class="block text-caption text-neutrals-200"
              >
                Noté à {{ ligne.avant ? "l'entrée" : 'la sortie' }} seulement
              </span>
            </span>

            <!-- Avant → après, seulement si l'état a changé. -->
            <span
              v-if="ligne.avant && ligne.apres && ligne.evolution !== 'identique'"
              class="flex shrink-0 items-center gap-1"
              :aria-label="`${BADGES_ETAT[ligne.avant.etat].label} à l'entrée, ${BADGES_ETAT[ligne.apres.etat].label} à la sortie`"
            >
              <span
                class="rounded-full px-2 py-0.5 text-caption font-semibold opacity-70"
                :class="BADGES_ETAT[ligne.avant.etat].classes"
              >
                {{ BADGES_ETAT[ligne.avant.etat].label }}
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
                :class="BADGES_ETAT[ligne.apres.etat].classes"
              >
                {{ BADGES_ETAT[ligne.apres.etat].label }}
              </span>
            </span>
            <span
              v-else-if="ligne.apres ?? ligne.avant"
              class="shrink-0 rounded-full px-2 py-0.5 text-caption font-semibold"
              :class="BADGES_ETAT[(ligne.apres ?? ligne.avant)!.etat].classes"
            >
              {{ BADGES_ETAT[(ligne.apres ?? ligne.avant)!.etat].label }}
            </span>
            <span v-else class="shrink-0 text-caption text-neutrals-200">Non noté</span>
          </button>
        </li>
      </ul>
    </div>
  </section>
</template>
