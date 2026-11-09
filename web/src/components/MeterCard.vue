<script setup lang="ts">
/**
 * Fiche d'un compteur (tâche B6) : la fiche d'élément, avec en plus la saisie de
 * l'index. À la sortie, elle rappelle l'index de l'entrée et calcule la consommation.
 * Émet `valider` avec l'état, le commentaire et le relevé.
 */
import { computed, ref, watch } from 'vue'
import ElementCard, { type SaisieConstat } from '@/components/ElementCard.vue'
import type { Element, Releve, TypeEdl } from '@/types/edl'

const props = defineProps<{
  element: Element
  piece: string
  constat?: SaisieConstat | null
  x: number
  /** État des lieux en cours. */
  type: TypeEdl
  /** Relevé de l'entrée, pour calculer la consommation à la sortie. */
  releveEntree?: Releve | null
  /** Date ISO de l'état des lieux d'entrée. */
  dateEntree?: string | null
}>()

const emit = defineEmits<{
  valider: [saisie: SaisieConstat]
  fermer: []
}>()

const saisie = ref('')

watch(
  () => props.element.key,
  () => {
    const valeur = props.constat?.releve?.valeur
    saisie.value = valeur === undefined ? '' : String(valeur).replace('.', ',')
  },
  { immediate: true },
)

const unite = computed(
  () => props.element.unite ?? props.constat?.releve?.unite ?? props.releveEntree?.unite ?? '',
)

/** Index saisi, ou null s'il est vide ou mal écrit. Accepte la virgule et les espaces. */
const index = computed<number | null>(() => {
  const texte = saisie.value.replace(/[\s  ]/g, '').replace(',', '.')
  if (texte === '' || !/^\d+(\.\d+)?$/.test(texte)) return null
  return Number(texte)
})

const indexInvalide = computed(() => saisie.value.trim() !== '' && index.value === null)

/** Consommation entre l'entrée et la sortie, ou null si elle ne peut pas être calculée. */
const consommation = computed(() => {
  if (props.type !== 'sortie' || !props.releveEntree || index.value === null) return null
  return index.value - props.releveEntree.valeur
})

function formater(valeur: number): string {
  const nombre = valeur.toLocaleString('fr-FR', { maximumFractionDigits: 3 })
  return unite.value ? `${nombre} ${unite.value}` : nombre
}

function formaterDate(iso: string): string {
  return new Date(`${iso}T00:00:00`).toLocaleDateString('fr-FR')
}

function valider(s: SaisieConstat) {
  if (index.value === null) return
  emit('valider', { ...s, releve: { valeur: index.value, unite: unite.value } })
}
</script>

<template>
  <ElementCard
    :element="element"
    :piece="piece"
    :constat="constat"
    :x="x"
    :pret="index !== null"
    raison="Saisissez l'index du compteur"
    exemple="Ex. : compteur plombé, accessible"
    @valider="valider"
    @fermer="emit('fermer')"
  >
    <label class="flex flex-col gap-2">
      <span class="font-semibold">Index {{ type === 'entree' ? "à l'entrée" : 'à la sortie' }}</span>
      <span class="flex items-center gap-2">
        <input
          v-model="saisie"
          type="text"
          inputmode="decimal"
          autocomplete="off"
          class="h-11 min-w-0 flex-1 rounded-sm border px-3 text-right tabular-nums placeholder:text-neutrals-100 focus-visible:outline-none"
          :class="
            indexInvalide
              ? 'border-mauvais-300'
              : 'border-neutrals-50 focus-visible:border-neutrals-300'
          "
          placeholder="0"
          :aria-invalid="indexInvalide"
          aria-describedby="index-aide"
        />
        <span v-if="unite" class="shrink-0 font-semibold">{{ unite }}</span>
      </span>
      <span id="index-aide" class="text-caption" :class="indexInvalide ? 'text-mauvais-500' : 'text-neutrals-200'">
        <template v-if="indexInvalide">Chiffres seulement, avec une virgule si besoin.</template>
        <template v-else-if="type === 'sortie' && releveEntree">
          À l'entrée{{ dateEntree ? ` (${formaterDate(dateEntree)})` : '' }}&nbsp;:
          {{ formater(releveEntree.valeur) }}
        </template>
        <template v-else-if="type === 'sortie'">Aucun index relevé à l'entrée.</template>
        <template v-else>Le chiffre affiché sur le compteur.</template>
      </span>
    </label>

    <p
      v-if="consommation !== null"
      class="rounded-sm px-3 py-2"
      :class="consommation < 0 ? 'bg-mauvais-100 text-mauvais-500' : 'bg-neutrals-50/40'"
      role="status"
    >
      <template v-if="consommation < 0">
        L'index est plus bas qu'à l'entrée. Vérifiez la saisie.
      </template>
      <template v-else>
        Consommation depuis l'entrée&nbsp;:
        <strong class="font-semibold">{{ formater(consommation) }}</strong>
      </template>
    </p>
  </ElementCard>
</template>
