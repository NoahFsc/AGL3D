<script setup lang="ts">
/** Page de démo : la 3D Unity dans une page AGL, avec l'écran d'accueil par-dessus. */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import AglShell from '@/components/AglShell.vue'
import ElementCard, { type SaisieConstat } from '@/components/ElementCard.vue'
import HomeScreen, { type ModeChoisi } from '@/components/HomeScreen.vue'
import RoomButtons from '@/components/RoomButtons.vue'
import UnityViewer from '@/components/UnityViewer.vue'
import { edlRepository } from '@/repositories'
import type { CibleCamera, ModeAffichage, PointPayload, UnityToVueMessage } from '@/types/bridge'
import type { Constat, Dossier, Edl, ElementKey, Etat, TypeEdl } from '@/types/edl'

const DOSSIER_ID = 'appart-demo'

const viewer = ref<InstanceType<typeof UnityViewer> | null>(null)
const received = ref<UnityToVueMessage[]>([])

const dossier = ref<Dossier | null>(null)
/** Erreur de lecture du dossier, affichée au-dessus de la 3D. */
const erreurDossier = ref<string | null>(null)
const edls = ref<Edl[]>([])
const titre = computed(() => dossier.value?.logement.libelle ?? '')
const adresse = computed(() => dossier.value?.logement.adresse ?? '')
const mode = ref<ModeAffichage>('accueil')
const unityPret = ref(false)
const pieces = computed(() => dossier.value?.pieces ?? [])
/** Pièce où se trouve la caméra, mise à jour par `roomChanged`. */
const pieceCourante = ref<CibleCamera | null>(null)

const entreeFaite = computed(() =>
  edls.value.some((edl) => edl.type === 'entree' && edl.constats.length > 0),
)

/** Charge le logement et vérifie si l'entrée est déjà faite. */
onMounted(async () => {
  try {
    const [d, e] = await Promise.all([
      edlRepository.getDossier(DOSSIER_ID),
      edlRepository.listEdls(DOSSIER_ID),
    ])
    dossier.value = d
    edls.value = e
  } catch (e) {
    console.error('[AGL3D] Dossier illisible', e)
    erreurDossier.value = e instanceof Error ? e.message : String(e)
  }
})

const LIBELLES_MODE: Record<ModeAffichage, string> = {
  accueil: 'Maquette 3D',
  entree: 'Entrée',
  sortie: 'Sortie',
  comparer: 'Comparer',
}

const filAriane = computed(() => ['États des lieux', titre.value, LIBELLES_MODE[mode.value]])

/** Envoie le mode courant à Unity (`init`). */
function envoyerMode() {
  viewer.value?.send('init', { dossierId: DOSSIER_ID, mode: mode.value })
}

/** Retient le mode choisi ; il part vers Unity tout de suite ou à `ready`. */
function choisir(nouveauMode: ModeChoisi) {
  selection.value = null
  mode.value = nouveauMode
  if (unityPret.value) envoyerMode()
}

/* ------------------------------ Fiche (B4) ------------------------------ */

/** Élément dont la fiche est ouverte, avec l'abscisse du clic pour placer la fiche. */
const selection = ref<{ key: ElementKey; x: number } | null>(null)

/** La fiche s'ouvre seulement pendant une entrée ou une sortie. */
const typeEdl = computed<TypeEdl | null>(() =>
  mode.value === 'entree' || mode.value === 'sortie' ? mode.value : null,
)

const edlCourant = computed(() => edls.value.find((edl) => edl.type === typeEdl.value) ?? null)

const elementSelectionne = computed(() => {
  const key = selection.value?.key
  return key ? (dossier.value?.elements.find((e) => e.key === key) ?? null) : null
})

const pieceSelectionnee = computed(() => {
  const pieceKey = elementSelectionne.value?.pieceKey
  return dossier.value?.pieces.find((p) => p.key === pieceKey)?.label ?? ''
})

const constatSelectionne = computed(
  () => edlCourant.value?.constats.find((c) => c.elementKey === selection.value?.key) ?? null,
)

/**
 * Ouvre la fiche de l'élément cliqué. Sur l'accueil, où Entrée est déjà mis en avant,
 * le clic lance l'entrée. En mode Comparer, la sélection est retirée.
 */
function ouvrirFiche(key: ElementKey, x: number) {
  const connu = dossier.value?.elements.some((e) => e.key === key)
  if (connu && mode.value === 'accueil') {
    // `init` retire la sélection dans Unity : on la redonne juste après.
    choisir('entree')
    viewer.value?.send('select', { key })
  }
  if (!typeEdl.value || !connu) {
    if (!connu) console.warn(`[AGL3D] Élément inconnu du dossier : ${key}`)
    selection.value = null
    viewer.value?.send('select', { key: null })
    return
  }
  selection.value = { key, x }
}

/** Ferme la fiche (croix, Échap) et retire la surbrillance dans la 3D. */
function fermerFiche() {
  selection.value = null
  viewer.value?.send('select', { key: null })
}

const LIBELLES_ETAT: Record<Etat, string> = { bon: 'Bon', usage: 'Usage', mauvais: 'Mauvais' }

/**
 * Enregistre le constat dans l'état des lieux en cours, met à jour les repères et
 * confirme l'enregistrement. En cas d'échec, la fiche reste ouverte.
 */
async function validerFiche(saisie: SaisieConstat) {
  const key = selection.value?.key
  const type = typeEdl.value
  const element = elementSelectionne.value
  if (!key || !type || !element) return

  // On garde la photo et le relevé d'un constat existant : la fiche ne les modifie pas.
  const ancien = constatSelectionne.value
  const constat: Constat = { ...ancien, elementKey: key, ...saisie, photo: ancien?.photo ?? null }
  try {
    const edl = await edlRepository.saveConstat(DOSSIER_ID, type, constat)
    edls.value = [...edls.value.filter((e) => e.type !== type), edl]
  } catch (e) {
    console.error('[AGL3D] Constat non enregistré', e)
    afficherMessage("Le constat n'a pas pu être enregistré. Réessayez.", true)
    return
  }

  envoyerPoints()
  fermerFiche()
  afficherMessage(`${element.label} : ${LIBELLES_ETAT[saisie.etat]}, enregistré.`)
}

/* -------------------------- Jeu de démo (B5) --------------------------- */

const nombreConstats = computed(() => edls.value.reduce((n, edl) => n + edl.constats.length, 0))

/** Remplace les constats enregistrés par ceux du jeu de démo, après confirmation. */
async function chargerDemo() {
  if (
    nombreConstats.value > 0 &&
    !window.confirm('Remplacer les constats enregistrés sur cet appareil par le jeu de démo ?')
  ) {
    return
  }
  try {
    edls.value = await edlRepository.loadDemo(DOSSIER_ID)
  } catch (e) {
    console.error('[AGL3D] Jeu de démo illisible', e)
    afficherMessage("Le jeu de démo n'a pas pu être chargé.", true)
    return
  }
  apresChangementEdls()
  afficherMessage('Jeu de démo chargé : entrée et sortie.')
}

/** Efface tous les constats enregistrés, après confirmation, et revient à l'accueil. */
async function toutEffacer() {
  if (!window.confirm('Effacer tous les constats enregistrés sur cet appareil ?')) return
  await edlRepository.reset(DOSSIER_ID)
  edls.value = []
  mode.value = 'accueil'
  apresChangementEdls()
  afficherMessage('Constats effacés.')
}

/** Ferme la fiche et renvoie le mode et les repères à Unity. */
function apresChangementEdls() {
  if (selection.value) fermerFiche()
  if (!unityPret.value) return
  envoyerMode()
  if (typeEdl.value) envoyerPoints()
}

/* ------------------------------ Message ------------------------------- */

/** Message court en bas de l'écran, retour visuel de chaque action. */
const message = ref<{ texte: string; erreur: boolean } | null>(null)
let minuterieMessage: ReturnType<typeof setTimeout> | undefined

function afficherMessage(texte: string, erreur = false) {
  message.value = { texte, erreur }
  clearTimeout(minuterieMessage)
  minuterieMessage = setTimeout(() => (message.value = null), 4000)
}

onBeforeUnmount(() => clearTimeout(minuterieMessage))

/** Envoie à Unity les repères des éléments notés dans l'état des lieux en cours. */
function envoyerPoints() {
  const elements = dossier.value?.elements ?? []
  const points: PointPayload[] = (edlCourant.value?.constats ?? []).map((c) => ({
    key: c.elementKey,
    label: elements.find((e) => e.key === c.elementKey)?.label ?? c.elementKey,
    etat: c.etat,
    degrade: false,
  }))
  viewer.value?.send('setPoints', { points })
}

/** Déplace la caméra vers une pièce ; Unity répond par `roomChanged` à l'arrivée. */
function allerDans(room: CibleCamera) {
  viewer.value?.send('focusRoom', { room })
}

/**
 * Journalise les messages Unity, envoie le mode à `ready`, ouvre ou ferme la fiche
 * et suit la pièce courante.
 */
function onMessage(message: UnityToVueMessage) {
  console.info('[AGL3D] Unity -> Vue', message)
  received.value.unshift(message)

  switch (message.type) {
    case 'ready':
      unityPret.value = true
      envoyerMode()
      break
    case 'pointSelected':
      ouvrirFiche(message.payload.key, message.payload.x)
      break
    case 'deselected':
      selection.value = null
      break
    case 'roomChanged':
      pieceCourante.value = message.payload.room
      break
  }
}
</script>

<template>
  <AglShell :fil-ariane="filAriane" :titre="titre" :sous-titre="adresse">
    <div
      v-if="erreurDossier"
      class="flex items-start gap-3 rounded-md bg-mauvais-100 px-4 py-3 text-mauvais-500"
      role="alert"
    >
      <!-- Lucide « triangle-alert » -->
      <svg
        class="mt-0.5 shrink-0"
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
        <path
          d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"
        />
        <path d="M12 9v4" />
        <path d="M12 17h.01" />
      </svg>
      <div class="min-w-0">
        <p class="font-semibold">Les données du logement sont illisibles.</p>
        <p class="text-caption whitespace-pre-line">{{ erreurDossier }}</p>
      </div>
    </div>

    <section class="overflow-hidden rounded-lg border border-neutrals-50">
      <div class="relative md:[&_.viewer]:min-h-[660px] max-md:[&_.viewer]:aspect-[4/5]!">
        <UnityViewer ref="viewer" @message="onMessage" />
        <HomeScreen :mode="mode" :entree-faite="entreeFaite" @choisir="choisir" />
        <RoomButtons
          v-if="mode !== 'accueil'"
          :pieces="pieces"
          :courante="pieceCourante"
          @focus="allerDans"
        />
        <ElementCard
          v-if="elementSelectionne"
          :element="elementSelectionne"
          :piece="pieceSelectionnee"
          :constat="constatSelectionne"
          :x="selection?.x ?? 0"
          @valider="validerFiche"
          @fermer="fermerFiche"
        />
      </div>
    </section>

    <div class="flex flex-wrap items-center gap-x-6 gap-y-2">
      <button
        type="button"
        class="inline-flex h-9 cursor-pointer items-center justify-center rounded-sm border-2 border-primary-300 bg-white px-4 font-semibold text-primary-300 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300 disabled:cursor-not-allowed disabled:border-neutrals-50 disabled:text-neutrals-100"
        :disabled="!dossier"
        @click="chargerDemo"
      >
        Charger le jeu de démo
      </button>
      <button
        type="button"
        class="cursor-pointer font-semibold text-primary-300 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300 disabled:cursor-not-allowed disabled:text-neutrals-100 disabled:no-underline"
        :disabled="nombreConstats === 0"
        @click="toutEffacer"
      >
        Tout effacer
      </button>
      <p class="text-neutrals-200">
        {{ nombreConstats === 0 ? 'Aucun constat' : `${nombreConstats} constat${nombreConstats > 1 ? 's' : ''}` }}
        enregistré{{ nombreConstats > 1 ? 's' : '' }} sur cet appareil
      </p>
    </div>

    <details class="text-caption text-neutrals-200">
      <summary>Messages reçus de Unity ({{ received.length }})</summary>
      <p v-if="received.length === 0">Aucun message pour l'instant.</p>
      <ul v-else class="pl-4">
        <li v-for="(message, index) in received" :key="index">
          <code>{{ message.type }}</code> {{ JSON.stringify(message.payload) }}
        </li>
      </ul>
    </details>

    <div
      class="pointer-events-none fixed inset-x-0 bottom-6 z-50 flex justify-center px-4"
      aria-live="polite"
    >
      <p
        v-if="message"
        class="flex items-center gap-2 rounded-md bg-neutrals-400 px-4 py-3 text-white shadow-raised"
      >
        <!-- Lucide « circle-alert » ou « check » -->
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
          <template v-if="message.erreur">
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4" />
            <path d="M12 16h.01" />
          </template>
          <path v-else d="M20 6 9 17l-5-5" />
        </svg>
        {{ message.texte }}
      </p>
    </div>
  </AglShell>
</template>
