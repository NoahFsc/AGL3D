<script setup lang="ts">
/**
 * Habillage de l'application AGL repris de Figma (menu, fil d'Ariane, en-tête), autour
 * du contenu passé dans le slot. Menu réductible sur ordinateur, burger sur mobile.
 */
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import logo from '@/assets/agl/logo.svg'
import deconnexion from '@/assets/agl/deconnexion.svg'
import fermer from '@/assets/agl/fermer.svg'
import horsConnexion from '@/assets/agl/hors-connexion.svg'
import menu from '@/assets/agl/menu.svg'
import notifications from '@/assets/agl/notifications.svg'
import ouvrirMenu from '@/assets/agl/ouvrir-menu.svg'
import parametres from '@/assets/agl/parametres.svg'
import reduireMenu from '@/assets/agl/reduire-menu.svg'
import separateur from '@/assets/agl/separateur.svg'
import navTableauDeBord from '@/assets/agl/nav-tableau-de-bord.svg'
import navPatrimoine from '@/assets/agl/nav-patrimoine.svg'
import navLocataires from '@/assets/agl/nav-locataires.svg'
import navFinances from '@/assets/agl/nav-finances.svg'
import navDemandes from '@/assets/agl/nav-demandes.svg'
import navEtatsDesLieux from '@/assets/agl/nav-etats-des-lieux.svg'
import navEcheances from '@/assets/agl/nav-echeances.svg'
import navMessagerie from '@/assets/agl/nav-messagerie.svg'

defineProps<{
  filAriane: string[]
  titre: string
  sousTitre?: string
}>()

const navigation = [
  { label: 'Tableau de bord', icone: navTableauDeBord },
  { label: 'Patrimoine', icone: navPatrimoine },
  { label: 'Locataires', icone: navLocataires },
  { label: 'Finances', icone: navFinances },
  { label: 'Demandes', icone: navDemandes, compteur: 3 },
  { label: 'États des lieux', icone: navEtatsDesLieux, actif: true },
  { label: 'Échéances', icone: navEcheances },
  { label: 'Messagerie', icone: navMessagerie, compteur: 2 },
]

const CLE_MENU_REDUIT = 'agl3d:menu-reduit'

const reduit = ref(false)
const ouvertMobile = ref(false)

/** Reprend le choix enregistré, sinon réduit le menu sur tablette. */
onMounted(() => {
  const choix = lirePreference()
  reduit.value = choix === null ? window.innerWidth < 1024 : choix === '1'
  document.addEventListener('keydown', fermerAvecEchap)
})

onBeforeUnmount(() => document.removeEventListener('keydown', fermerAvecEchap))

/** Lit le choix « menu réduit » du navigateur, `null` s'il n'y en a pas. */
function lirePreference(): string | null {
  try {
    return localStorage.getItem(CLE_MENU_REDUIT)
  } catch {
    return null
  }
}

/** Réduit ou ouvre le menu et retient ce choix. */
function basculerMenu() {
  reduit.value = !reduit.value
  try {
    localStorage.setItem(CLE_MENU_REDUIT, reduit.value ? '1' : '0')
  } catch (e) {
    console.warn('[AglShell] Choix du menu non enregistré', e)
  }
}

/** Ferme le menu mobile avec Échap. */
function fermerAvecEchap(event: KeyboardEvent) {
  if (event.key === 'Escape') ouvertMobile.value = false
}

watch(ouvertMobile, (ouvert) => document.body.classList.toggle('overflow-hidden', ouvert))

const classeItem =
  'group relative flex w-full cursor-pointer items-center gap-3 rounded-md p-3 text-left font-medium whitespace-nowrap text-white hover:bg-white/8 focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-white'
const classeInfobulle =
  'pointer-events-none absolute top-1/2 left-[calc(100%+12px)] z-50 hidden -translate-y-1/2 rounded-sm bg-neutrals-400 px-2 py-1 text-caption font-normal whitespace-nowrap text-white before:absolute before:top-1/2 before:right-full before:-translate-y-1/2 before:border-5 before:border-transparent before:border-r-neutrals-400 before:content-[""]'
</script>

<template>
  <div class="flex min-h-screen">
    <aside
      id="agl-menu"
      aria-label="Menu AGL"
      class="fixed inset-y-0 left-0 z-40 flex w-[min(300px,85vw)] flex-col gap-8 overflow-y-auto bg-primary-300 px-6 py-8 text-white transition-[width,translate] duration-200 md:sticky md:top-0 md:h-screen md:shrink-0 md:translate-x-0 md:overflow-visible"
      :class="[
        ouvertMobile ? 'translate-x-0' : '-translate-x-full',
        reduit ? 'md:w-[92px] md:items-center' : 'md:w-[300px]',
      ]"
    >
      <div class="flex items-center gap-3 self-stretch" :class="{ 'md:justify-center': reduit }">
        <span class="relative size-10 shrink-0">
          <img :src="logo" alt="" class="absolute top-[2.4px] left-[4.59px]" />
        </span>
        <span class="flex flex-1 flex-col whitespace-nowrap" :class="{ 'md:hidden': reduit }">
          <strong class="text-h3 font-semibold">AGL</strong>
          <small class="text-caption">Aide à la Gestion Locative</small>
        </span>
        <button
          type="button"
          class="flex cursor-pointer rounded-sm focus-visible:outline-2 focus-visible:outline-white md:hidden"
          aria-label="Fermer le menu"
          @click="ouvertMobile = false"
        >
          <img :src="fermer" alt="" />
        </button>
      </div>

      <nav
        class="flex flex-col gap-1 self-stretch"
        :class="{ 'md:items-center': reduit }"
        aria-label="Navigation principale"
      >
        <a
          v-for="item in navigation"
          :key="item.label"
          href="#"
          :aria-label="item.label"
          :class="[
            classeItem,
            {
              'bg-white/16 font-semibold hover:bg-white/16': item.actif,
              'md:h-11 md:w-auto': reduit,
            },
          ]"
          :aria-current="item.actif ? 'page' : undefined"
          @click.prevent="ouvertMobile = false"
        >
          <img :src="item.icone" alt="" class="shrink-0" />
          <span class="flex-1 truncate" :class="{ 'md:hidden': reduit }">{{ item.label }}</span>
          <span
            v-if="item.compteur"
            class="rounded-full bg-accent-300 px-2 py-0.5 text-caption text-black"
            :class="{ 'md:hidden': reduit }"
          >
            {{ item.compteur }}
          </span>
          <span
            :class="[
              classeInfobulle,
              { 'md:group-hover:block md:group-focus-visible:block': reduit },
            ]"
            aria-hidden="true"
          >
            {{ item.label }}
          </span>
        </a>
      </nav>

      <div class="mt-auto flex flex-col gap-2 self-stretch" :class="{ 'md:items-center': reduit }">
        <button
          type="button"
          :class="[classeItem, 'max-md:hidden', { 'md:h-11 md:w-auto': reduit }]"
          :aria-label="reduit ? 'Ouvrir le menu' : 'Réduire le menu'"
          :aria-expanded="!reduit"
          aria-controls="agl-menu"
          @click="basculerMenu"
        >
          <img :src="reduit ? ouvrirMenu : reduireMenu" alt="" class="shrink-0" />
          <span class="flex-1 truncate" :class="{ 'md:hidden': reduit }">Réduire le menu</span>
          <span
            :class="[
              classeInfobulle,
              { 'md:group-hover:block md:group-focus-visible:block': reduit },
            ]"
            aria-hidden="true"
          >
            Ouvrir le menu
          </span>
        </button>
        <a
          href="#"
          aria-label="Paramètres"
          :class="[classeItem, { 'md:h-11 md:w-auto': reduit }]"
          @click.prevent
        >
          <img :src="parametres" alt="" class="shrink-0" />
          <span class="flex-1 truncate" :class="{ 'md:hidden': reduit }">Paramètres</span>
          <span
            :class="[
              classeInfobulle,
              { 'md:group-hover:block md:group-focus-visible:block': reduit },
            ]"
            aria-hidden="true"
          >
            Paramètres
          </span>
        </a>
        <div
          class="flex items-center gap-3 self-stretch rounded-md bg-white/10 p-3"
          :class="{ 'md:justify-center md:p-0.5': reduit }"
        >
          <span
            class="flex size-10 shrink-0 items-center justify-center rounded-full border border-neutrals-50 bg-white font-semibold text-primary-300"
            title="Claire Dubois, propriétaire"
          >
            CD
          </span>
          <span
            class="flex min-w-0 flex-1 flex-col whitespace-nowrap"
            :class="{ 'md:hidden': reduit }"
          >
            <strong class="font-semibold">Claire Dubois</strong>
            <small class="text-caption">Propriétaire</small>
          </span>
          <button
            type="button"
            class="flex cursor-pointer rounded-sm focus-visible:outline-2 focus-visible:outline-white"
            :class="{ 'md:hidden': reduit }"
            aria-label="Se déconnecter"
          >
            <img :src="deconnexion" alt="" />
          </button>
        </div>
      </div>
    </aside>

    <div
      class="fixed inset-0 z-30 bg-neutrals-400/40 transition-opacity duration-200 md:hidden"
      :class="ouvertMobile ? 'opacity-100' : 'pointer-events-none opacity-0'"
      aria-hidden="true"
      @click="ouvertMobile = false"
    ></div>

    <div
      class="flex min-w-0 flex-1 flex-col gap-4 px-4 pb-6 md:gap-8 md:px-6 md:py-8 lg:gap-10 lg:p-12"
    >
      <header
        class="sticky top-0 z-20 -mx-2 flex max-md:order-first h-14 items-center gap-1 bg-white md:hidden"
      >
        <button
          type="button"
          class="flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-md bg-white"
          aria-label="Ouvrir le menu"
          aria-controls="agl-menu"
          :aria-expanded="ouvertMobile"
          @click="ouvertMobile = true"
        >
          <img :src="menu" alt="" />
        </button>
        <p class="min-w-0 flex-1 truncate text-center text-h3 font-semibold">{{ titre }}</p>
        <button
          type="button"
          class="flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-md bg-white"
          aria-label="Notifications"
        >
          <img :src="notifications" alt="" />
        </button>
      </header>

      <div class="flex flex-wrap items-center justify-between gap-3 max-md:justify-start">
        <ol
          class="flex flex-wrap items-center text-neutrals-200 max-md:hidden"
          aria-label="Fil d'Ariane"
        >
          <li v-for="(niveau, index) in filAriane" :key="index" class="flex items-center">
            <img v-if="index > 0" :src="separateur" alt="" />
            <span
              :class="{ 'font-medium text-neutrals-400': index === filAriane.length - 1 }"
              :aria-current="index === filAriane.length - 1 ? 'page' : undefined"
            >
              {{ niveau }}
            </span>
          </li>
        </ol>

        <div class="ml-auto flex items-center gap-3 max-md:ml-0">
          <span
            class="flex items-center gap-1 rounded-full bg-neutrals-50/60 px-3 py-1 text-caption font-medium tracking-[0.96px] text-neutrals-300 uppercase md:whitespace-nowrap"
          >
            <img :src="horsConnexion" alt="" />
            Hors connexion · enregistré sur cet appareil
          </span>
          <button
            type="button"
            class="flex size-10 cursor-pointer items-center justify-center rounded-md border border-neutrals-50 bg-white max-md:hidden"
            aria-label="Notifications"
          >
            <img :src="notifications" alt="" />
          </button>
        </div>
      </div>

      <header class="flex flex-col gap-1 max-md:-order-1">
        <h1 class="text-h1 font-semibold max-md:hidden">{{ titre }}</h1>
        <p v-if="sousTitre" class="text-neutrals-200">{{ sousTitre }}</p>
      </header>

      <slot />
    </div>
  </div>
</template>
