<script setup lang="ts">
/**
 * Affiche le build Unity dans un canvas, avec un écran de chargement jusqu'à `ready`
 * et un message d'erreur compréhensible si le build ne démarre pas (tâche B8).
 */
import { computed, ref } from 'vue'
import { useUnity } from '@/composables/useUnity'
import type { TypeErreurChargement } from '@/composables/erreurChargement'
import type { UnityToVueMessage } from '@/types/bridge'

const emit = defineEmits<{ message: [message: UnityToVueMessage] }>()

const canvas = ref<HTMLCanvasElement | null>(null)

const { progress, isReady, error, send } = useUnity(canvas, {
  onMessage: (message) => emit('message', message),
})

defineExpose({ send, isReady })

/** Unity rapporte 0,9 à la fin du téléchargement, puis 1 quand il démarre. */
const etape = computed(() =>
  progress.value < 0.9 ? 'Téléchargement de la maquette' : 'Préparation de la maquette',
)
const pourcentage = computed(() => Math.round(progress.value * 100))

const MESSAGES: Record<
  TypeErreurChargement,
  { titre: string; texte: string; reessayer: boolean }
> = {
  webgl: {
    titre: 'Votre navigateur ne peut pas afficher la maquette',
    texte:
      "Ouvrez cette page avec une version récente de Chrome, Firefox, Edge ou Safari. Si c'est déjà le cas, vérifiez que l'accélération matérielle est activée.",
    reessayer: false,
  },
  introuvable: {
    titre: "La maquette n'est pas disponible",
    texte: "Ses fichiers sont absents du site. Prévenez l'équipe qui gère l'application.",
    reessayer: true,
  },
  reseau: {
    titre: 'Le téléchargement a été interrompu',
    texte: 'Vérifiez votre connexion à internet, puis réessayez.',
    reessayer: true,
  },
  muet: {
    titre: 'La maquette ne répond pas',
    texte: "Elle s'est chargée mais n'a pas démarré. Réessayez.",
    reessayer: true,
  },
  inconnu: {
    titre: "La maquette n'a pas pu démarrer",
    texte: "Réessayez. Si le problème continue, prévenez l'équipe qui gère l'application.",
    reessayer: true,
  },
}

const messageErreur = computed(() => (error.value ? MESSAGES[error.value.type] : null))

/** Recharger la page repart d'un état propre, ce que Unity ne permet pas sur place. */
function reessayer() {
  window.location.reload()
}
</script>

<template>
  <div class="viewer relative aspect-video w-full bg-white">
    <!-- Le canvas doit exister dès le montage : useUnity le lit dans onMounted.
         Unity exige un id : il retrouve le canvas avec querySelector('#<id>'). -->
    <canvas id="unity-canvas" ref="canvas" class="block size-full" tabindex="-1"></canvas>

    <div
      v-if="error && messageErreur"
      class="absolute inset-0 flex items-center justify-center bg-white p-6"
      role="alert"
    >
      <div class="flex max-w-md flex-col items-center gap-3 text-center">
        <span
          class="inline-flex size-12 items-center justify-center rounded-full bg-mauvais-100 text-mauvais-500"
          aria-hidden="true"
        >
          <!-- Lucide « circle-alert » -->
          <svg
            width="24"
            height="24"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4" />
            <path d="M12 16h.01" />
          </svg>
        </span>
        <h2 class="text-h3 font-semibold text-balance">{{ messageErreur.titre }}</h2>
        <p class="text-balance text-neutrals-200">{{ messageErreur.texte }}</p>
        <button
          v-if="messageErreur.reessayer"
          type="button"
          class="mt-2 inline-flex h-11 cursor-pointer items-center justify-center rounded-sm border-2 border-primary-300 bg-primary-300 px-5 font-semibold text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300"
          @click="reessayer"
        >
          Réessayer
        </button>
        <details class="mt-2 max-w-full text-caption text-neutrals-200">
          <summary class="cursor-pointer">Détail technique</summary>
          <p class="mt-1 break-words">{{ error.message }}</p>
          <p v-if="error.type === 'introuvable'" class="mt-1">
            En développement : le build Unity doit être dans <code>web/public/unity/</code>.
          </p>
        </details>
      </div>
    </div>

    <div
      v-else-if="!isReady"
      class="absolute inset-0 flex items-center justify-center bg-white p-6"
      role="status"
    >
      <div class="flex w-full max-w-xs flex-col items-center gap-3 text-center">
        <p class="font-semibold">{{ etape }}</p>
        <div
          class="h-2 w-full overflow-hidden rounded-full bg-neutrals-50"
          role="progressbar"
          :aria-valuenow="pourcentage"
          aria-valuemin="0"
          aria-valuemax="100"
          :aria-label="etape"
        >
          <div
            class="h-full rounded-full bg-primary-300 transition-[width] duration-300"
            :style="{ width: `${pourcentage}%` }"
          />
        </div>
        <p class="text-caption text-neutrals-200">
          {{ pourcentage }}&nbsp;%<br />
          Le premier chargement peut être un peu long.
        </p>
      </div>
    </div>
  </div>
</template>
