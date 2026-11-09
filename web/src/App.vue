<script setup lang="ts">
/** Page de démo : la 3D Unity dans une page AGL, avec l'écran d'accueil par-dessus. */
import { computed, onMounted, ref } from 'vue'
import AglShell from '@/components/AglShell.vue'
import ElementCard, { type SaisieConstat } from '@/components/ElementCard.vue'
import HomeScreen, { type ModeChoisi } from '@/components/HomeScreen.vue'
import RoomButtons from '@/components/RoomButtons.vue'
import UnityViewer from '@/components/UnityViewer.vue'
import { edlRepository } from '@/repositories'
import type { CibleCamera, ModeAffichage, PointPayload, UnityToVueMessage } from '@/types/bridge'
import type { Constat, Dossier, Edl, ElementKey, TypeEdl } from '@/types/edl'

const DOSSIER_ID = 'appart-demo'

const viewer = ref<InstanceType<typeof UnityViewer> | null>(null)
const received = ref<UnityToVueMessage[]>([])

const dossier = ref<Dossier | null>(null)
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

/**
 * Garde le constat de l'état des lieux en cours et met à jour les repères.
 * Pour l'instant en mémoire : l'enregistrement avec `saveConstat` arrive avec B5.
 */
function validerFiche(saisie: SaisieConstat) {
  const key = selection.value?.key
  const type = typeEdl.value
  if (!key || !type) return

  let edl = edls.value.find((e) => e.type === type)
  if (!edl) {
    edl = { type, date: new Date().toISOString().slice(0, 10), constats: [] }
    edls.value.push(edl)
  }
  const ancien = edl.constats.find((c) => c.elementKey === key)
  const constat: Constat = { ...ancien, elementKey: key, ...saisie, photo: ancien?.photo ?? null }
  edl.constats = [...edl.constats.filter((c) => c.elementKey !== key), constat]

  envoyerPoints()
  fermerFiche()
}

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

    <details class="text-caption text-neutrals-200">
      <summary>Messages reçus de Unity ({{ received.length }})</summary>
      <p v-if="received.length === 0">Aucun message pour l'instant.</p>
      <ul v-else class="pl-4">
        <li v-for="(message, index) in received" :key="index">
          <code>{{ message.type }}</code> {{ JSON.stringify(message.payload) }}
        </li>
      </ul>
    </details>
  </AglShell>
</template>
