<script setup lang="ts">
/**
 * Changement de mode (C4) : les boutons Entrée, Sortie et Comparer de `HomeScreen`, et le
 * court message d'aide affiché après un clic sur Entrée. Émet `choisir` avec le mode
 * cliqué ; la page envoie alors `init`, `setPoints` et, pour Entrée, `focusRoom`.
 *
 * Le message disparaît après quelques secondes, dès qu'une fiche s'ouvre (l'utilisateur a
 * compris où cliquer) ou quand on quitte l'entrée.
 */
import { onBeforeUnmount, ref, watch } from 'vue'
import HomeScreen, { type ModeChoisi } from '@/components/HomeScreen.vue'
import type { ModeAffichage } from '@/types/bridge'

const props = defineProps<{
  mode: ModeAffichage
  entreeFaite: boolean
  /** Vrai quand une fiche est ouverte : le message d'aide n'a plus lieu d'être. */
  ficheOuverte: boolean
}>()

const emit = defineEmits<{ choisir: [mode: ModeChoisi] }>()

/** Durée d'affichage du message d'aide, en millisecondes. */
const DUREE_AIDE = 6000

const aideVisible = ref(false)
let minuterie: ReturnType<typeof setTimeout> | undefined

function masquerAide() {
  clearTimeout(minuterie)
  aideVisible.value = false
}

function choisir(mode: ModeChoisi) {
  emit('choisir', mode)
  masquerAide()
  if (mode !== 'entree') return
  aideVisible.value = true
  minuterie = setTimeout(masquerAide, DUREE_AIDE)
}

watch(
  () => props.mode,
  (mode) => {
    if (mode !== 'entree') masquerAide()
  },
)

watch(
  () => props.ficheOuverte,
  (ouverte) => {
    if (ouverte) masquerAide()
  },
)

onBeforeUnmount(() => clearTimeout(minuterie))
</script>

<template>
  <HomeScreen :mode="mode" :entree-faite="entreeFaite" @choisir="choisir" />
  <div
    class="pointer-events-none absolute inset-x-0 top-16 flex justify-center px-4 max-md:top-20"
    aria-live="polite"
  >
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="-translate-y-1 opacity-0"
      leave-active-class="transition duration-300 ease-in"
      leave-to-class="opacity-0"
    >
      <p
        v-if="aideVisible"
        class="flex items-center gap-2 rounded-md bg-neutrals-400 px-4 py-3 text-white shadow-raised"
      >
        <!-- Lucide « mouse-pointer-click » -->
        <svg
          class="shrink-0"
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
          <path d="M14 4.1 12 6" />
          <path d="m5.1 8-2.9-.8" />
          <path d="m6 12-1.9 2" />
          <path d="M7.2 2.2 8 5.1" />
          <path
            d="M9.037 9.69a.498.498 0 0 1 .653-.653l11 4.5a.5.5 0 0 1-.074.949l-4.349 1.041a1 1 0 0 0-.74.739l-1.04 4.35a.5.5 0 0 1-.95.074z"
          />
        </svg>
        Survolez un élément, cliquez pour le noter.
      </p>
    </Transition>
  </div>
</template>
