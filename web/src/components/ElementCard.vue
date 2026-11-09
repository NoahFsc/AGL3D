<script setup lang="ts">
/**
 * Fiche d'un élément cliqué dans la 3D (tâche B4) : nom, pièce, choix de l'état,
 * commentaire et bouton Valider. Échap ou la croix ferment la fiche.
 * La fiche se cale sur le bord opposé au point cliqué pour ne pas cacher l'élément,
 * entre les boutons de mode en haut et les boutons de pièce en bas.
 * Émet `valider` avec l'état et le commentaire, `fermer` sans rien enregistrer.
 */
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { Element, Etat } from '@/types/edl'

export interface SaisieConstat {
  etat: Etat
  commentaire: string
}

const props = defineProps<{
  element: Element
  piece: string
  /** Constat déjà enregistré pour cet élément, pour préremplir la fiche. */
  constat?: SaisieConstat | null
  /** Abscisse du point cliqué, de 0 à 1 dans le canvas. */
  x: number
}>()

const emit = defineEmits<{
  valider: [saisie: SaisieConstat]
  fermer: []
}>()

const OPTIONS: { etat: Etat; label: string; aide: string; classes: string; pastille: string }[] = [
  {
    etat: 'bon',
    label: 'Bon',
    aide: 'Propre, sans défaut visible',
    classes: 'has-checked:border-bon-300 has-checked:bg-bon-100 has-checked:text-bon-500',
    pastille: 'bg-bon-300',
  },
  {
    etat: 'usage',
    label: 'Usage',
    aide: 'Usure normale, petites traces',
    classes: 'has-checked:border-usage-300 has-checked:bg-usage-100 has-checked:text-usage-500',
    pastille: 'bg-usage-300',
  },
  {
    etat: 'mauvais',
    label: 'Mauvais',
    aide: 'Dégradé, à réparer ou remplacer',
    classes:
      'has-checked:border-mauvais-300 has-checked:bg-mauvais-100 has-checked:text-mauvais-500',
    pastille: 'bg-mauvais-300',
  },
]

const etat = ref<Etat | null>(null)
const commentaire = ref('')
const titre = ref<HTMLElement | null>(null)

/** Préremplit la fiche à l'ouverture et quand on clique un autre élément. */
watch(
  () => props.element.key,
  async () => {
    etat.value = props.constat?.etat ?? null
    commentaire.value = props.constat?.commentaire ?? ''
    await nextTick()
    titre.value?.focus()
  },
  { immediate: true },
)

/** Point cliqué à droite : la fiche passe à gauche. */
const aGauche = computed(() => props.x > 0.55)

function valider() {
  if (!etat.value) return
  emit('valider', { etat: etat.value, commentaire: commentaire.value.trim() })
}

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
    :aria-labelledby="`fiche-${element.key}`"
  >
    <header class="flex items-start gap-3 px-5 pt-5 pb-3">
      <div class="min-w-0 flex-1">
        <h2
          :id="`fiche-${element.key}`"
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

    <form
      class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-5 pb-5"
      @submit.prevent="valider"
    >
      <fieldset class="flex flex-col gap-2">
        <legend class="mb-2 font-semibold">État</legend>
        <label
          v-for="option in OPTIONS"
          :key="option.etat"
          class="flex cursor-pointer items-center gap-3 rounded-sm border-2 border-neutrals-50 px-3 py-2 has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-accent-300"
          :class="option.classes"
        >
          <input v-model="etat" type="radio" name="etat" :value="option.etat" class="sr-only" />
          <span class="size-4 shrink-0 rounded-full" :class="option.pastille" aria-hidden="true" />
          <span class="flex flex-col">
            <span class="font-semibold">{{ option.label }}</span>
            <span class="text-caption">{{ option.aide }}</span>
          </span>
        </label>
      </fieldset>

      <label class="flex flex-col gap-2">
        <span class="font-semibold">Commentaire</span>
        <textarea
          v-model="commentaire"
          rows="2"
          maxlength="500"
          class="resize-none rounded-sm border border-neutrals-50 px-3 py-2 placeholder:text-neutrals-100 focus-visible:border-neutrals-300 focus-visible:outline-none"
          placeholder="Ex. : rayure de 10 cm sous la fenêtre"
        />
      </label>

      <button
        type="submit"
        class="mt-auto inline-flex h-11 shrink-0 items-center justify-center rounded-sm border-2 px-4 font-semibold focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300"
        :class="
          etat
            ? 'cursor-pointer border-primary-300 bg-primary-300 text-white'
            : 'cursor-not-allowed border-neutrals-50 bg-white text-neutrals-100'
        "
        :disabled="!etat"
        :title="etat ? '' : 'Choisissez d\'abord un état'"
      >
        Valider
      </button>
    </form>
  </aside>
</template>
