<script setup lang="ts">
/** Page de démo : la 3D Unity dans une page AGL, avec l'écran d'accueil par-dessus. */
import { computed, onMounted, ref } from 'vue'
import AglShell from '@/components/AglShell.vue'
import HomeScreen, { type ModeChoisi } from '@/components/HomeScreen.vue'
import UnityViewer from '@/components/UnityViewer.vue'
import { edlRepository } from '@/repositories'
import type { ModeAffichage, UnityToVueMessage } from '@/types/bridge'

const DOSSIER_ID = 'appart-demo'

const viewer = ref<InstanceType<typeof UnityViewer> | null>(null)
const received = ref<UnityToVueMessage[]>([])

const titre = ref('')
const adresse = ref('')
const mode = ref<ModeAffichage>('accueil')
const entreeFaite = ref(false)
const unityPret = ref(false)

/** Charge le logement et vérifie si l'entrée est déjà faite. */
onMounted(async () => {
  try {
    const [dossier, edls] = await Promise.all([
      edlRepository.getDossier(DOSSIER_ID),
      edlRepository.listEdls(DOSSIER_ID),
    ])
    titre.value = dossier.logement.libelle
    adresse.value = dossier.logement.adresse
    entreeFaite.value = edls.some((edl) => edl.type === 'entree' && edl.constats.length > 0)
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
  mode.value = nouveauMode
  if (unityPret.value) envoyerMode()
}

/** Journalise les messages Unity et envoie le mode à `ready`. */
function onMessage(message: UnityToVueMessage) {
  console.info('[AGL3D] Unity -> Vue', message)
  received.value.unshift(message)

  if (message.type === 'ready') {
    unityPret.value = true
    envoyerMode()
  }
}
</script>

<template>
  <AglShell :fil-ariane="filAriane" :titre="titre" :sous-titre="adresse">
    <section class="overflow-hidden rounded-lg border border-neutrals-50">
      <div class="relative max-md:[&_.viewer]:aspect-[4/5]!">
        <UnityViewer ref="viewer" @message="onMessage" />
        <HomeScreen :mode="mode" :entree-faite="entreeFaite" @choisir="choisir" />
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
