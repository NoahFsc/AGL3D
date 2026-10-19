<script setup lang="ts">
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

/*
 * Habillage de l'application AGL (menu latéral, fil d'Ariane, en-tête de page), repris
 * de la maquette Figma « Propriétaire · 09.1 · État des lieux ». Il simule la page qui
 * accueillera la 3D : les liens ne mènent nulle part.
 *
 * - Ordinateur : menu de 300 px, que « Réduire le menu » replie en 92 px (icônes seules,
 *   libellé en infobulle au survol). Le choix est retenu dans le navigateur.
 * - Tablette (< 1024 px) : le menu démarre replié.
 * - Mobile (< 768 px) : barre de 56 px avec le titre centré, le menu s'ouvre par-dessus
 *   la page avec le bouton burger.
 */

defineProps<{
  /** Niveaux du fil d'Ariane ; le dernier est la page courante. */
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

onMounted(() => {
  let choix: string | null = null
  try {
    choix = localStorage.getItem(CLE_MENU_REDUIT)
  } catch {
    // Stockage indisponible : on garde la valeur par défaut.
  }
  reduit.value = choix === null ? window.innerWidth < 1024 : choix === '1'
  document.addEventListener('keydown', fermerAvecEchap)
})

onBeforeUnmount(() => document.removeEventListener('keydown', fermerAvecEchap))

function basculerMenu() {
  reduit.value = !reduit.value
  try {
    localStorage.setItem(CLE_MENU_REDUIT, reduit.value ? '1' : '0')
  } catch {
    // Préférence non retenue : sans conséquence.
  }
}

function fermerAvecEchap(event: KeyboardEvent) {
  if (event.key === 'Escape') ouvertMobile.value = false
}

// Le menu mobile ouvert bloque le défilement de la page derrière lui.
watch(ouvertMobile, (ouvert) => document.body.classList.toggle('agl-menu-ouvert', ouvert))
</script>

<template>
  <div class="agl" :class="{ 'agl--reduit': reduit, 'agl--mobile-ouvert': ouvertMobile }">
    <aside id="agl-menu" class="sidebar" aria-label="Menu AGL">
      <div class="sidebar__marque">
        <span class="sidebar__logo"><img :src="logo" alt="" /></span>
        <span class="sidebar__nom">
          <strong>AGL</strong>
          <small>Aide à la Gestion Locative</small>
        </span>
        <button
          type="button"
          class="sidebar__fermer"
          aria-label="Fermer le menu"
          @click="ouvertMobile = false"
        >
          <img :src="fermer" alt="" />
        </button>
      </div>

      <nav class="sidebar__nav" aria-label="Navigation principale">
        <a
          v-for="item in navigation"
          :key="item.label"
          href="#"
          class="sidebar__item"
          :class="{ 'sidebar__item--actif': item.actif }"
          :aria-current="item.actif ? 'page' : undefined"
          @click.prevent="ouvertMobile = false"
        >
          <img :src="item.icone" alt="" class="sidebar__icone" />
          <span class="sidebar__label">{{ item.label }}</span>
          <span v-if="item.compteur" class="sidebar__compteur">{{ item.compteur }}</span>
          <span class="sidebar__infobulle" aria-hidden="true">{{ item.label }}</span>
        </a>
      </nav>

      <div class="sidebar__bas">
        <button
          type="button"
          class="sidebar__item sidebar__bascule"
          :aria-expanded="!reduit"
          aria-controls="agl-menu"
          @click="basculerMenu"
        >
          <img :src="reduit ? ouvrirMenu : reduireMenu" alt="" class="sidebar__icone" />
          <span class="sidebar__label">{{ reduit ? 'Ouvrir le menu' : 'Réduire le menu' }}</span>
          <span class="sidebar__infobulle" aria-hidden="true">
            {{ reduit ? 'Ouvrir le menu' : 'Réduire le menu' }}
          </span>
        </button>
        <a href="#" class="sidebar__item" @click.prevent>
          <img :src="parametres" alt="" class="sidebar__icone" />
          <span class="sidebar__label">Paramètres</span>
          <span class="sidebar__infobulle" aria-hidden="true">Paramètres</span>
        </a>
        <div class="sidebar__utilisateur">
          <span class="sidebar__avatar" title="Claire Dubois, propriétaire">CD</span>
          <span class="sidebar__identite">
            <strong>Claire Dubois</strong>
            <small>Propriétaire</small>
          </span>
          <button type="button" class="sidebar__deconnexion" aria-label="Se déconnecter">
            <img :src="deconnexion" alt="" />
          </button>
        </div>
      </div>
    </aside>

    <div class="agl__voile" aria-hidden="true" @click="ouvertMobile = false"></div>

    <div class="main">
      <!-- Mobile : Page Header Platform=Mobile, avec le burger à la place du retour. -->
      <header class="barre-mobile">
        <button
          type="button"
          class="barre-mobile__bouton"
          aria-label="Ouvrir le menu"
          aria-controls="agl-menu"
          :aria-expanded="ouvertMobile"
          @click="ouvertMobile = true"
        >
          <img :src="menu" alt="" />
        </button>
        <p class="barre-mobile__titre">{{ titre }}</p>
        <button type="button" class="barre-mobile__bouton" aria-label="Notifications">
          <img :src="notifications" alt="" />
        </button>
      </header>

      <div class="topbar">
        <ol class="fil" aria-label="Fil d'Ariane">
          <li v-for="(niveau, index) in filAriane" :key="index" class="fil__niveau">
            <img v-if="index > 0" :src="separateur" alt="" class="fil__separateur" />
            <span
              :class="{ fil__courant: index === filAriane.length - 1 }"
              :aria-current="index === filAriane.length - 1 ? 'page' : undefined"
            >
              {{ niveau }}
            </span>
          </li>
        </ol>

        <div class="topbar__droite">
          <span class="badge">
            <img :src="horsConnexion" alt="" />
            Hors connexion · enregistré sur cet appareil
          </span>
          <button type="button" class="topbar__notifications" aria-label="Notifications">
            <img :src="notifications" alt="" />
          </button>
        </div>
      </div>

      <!-- Organisms/Navigation/Page Header -->
      <header class="entete">
        <h1 class="entete__titre">{{ titre }}</h1>
        <p v-if="sousTitre" class="entete__sous-titre">{{ sousTitre }}</p>
      </header>

      <slot />
    </div>
  </div>
</template>

<style scoped>
.agl {
  display: flex;
  min-height: 100vh;
}

/* Organisms/Navigation/Sidebar */
.sidebar {
  position: sticky;
  top: 0;
  z-index: 2;
  display: flex;
  flex-direction: column;
  gap: 32px;
  flex-shrink: 0;
  width: 300px;
  height: 100vh;
  box-sizing: border-box;
  padding: 32px 24px;
  background: var(--sidebar-background);
  color: var(--sidebar-text-color);
  transition:
    width 0.2s ease,
    padding 0.2s ease;
}

.sidebar__marque {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sidebar__logo {
  position: relative;
  flex-shrink: 0;
  width: 40px;
  height: 40px;
}

.sidebar__logo img {
  position: absolute;
  top: 2.4px;
  left: 4.59px;
}

.sidebar__nom,
.sidebar__identite {
  display: flex;
  flex-direction: column;
  min-width: 0;
  white-space: nowrap;
}

.sidebar__nom {
  flex: 1;
}

.sidebar__nom strong {
  font-size: var(--size-h3);
  font-weight: 600;
  line-height: var(--line-height-h3);
}

.sidebar__nom small,
.sidebar__identite small {
  font-size: var(--size-caption);
  line-height: var(--line-height-caption);
}

.sidebar__fermer {
  display: none;
}

.sidebar__nav,
.sidebar__bas {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.sidebar__bas {
  gap: 8px;
  margin-top: auto;
}

.sidebar__item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 12px;
  box-sizing: border-box;
  width: 100%;
  padding: 12px;
  border: 0;
  border-radius: var(--radius-md);
  background: none;
  color: var(--sidebar-text-color);
  font: 500 var(--size-body) / var(--line-height-body) var(--font-family);
  text-align: left;
  text-decoration: none;
  white-space: nowrap;
  cursor: pointer;
}

.sidebar__item:hover {
  background: rgb(255 255 255 / 8%);
}

.sidebar__item:focus-visible,
.sidebar__deconnexion:focus-visible,
.sidebar__fermer:focus-visible {
  outline: 2px solid var(--color-neutrals-white);
  outline-offset: -2px;
}

.sidebar__item--actif,
.sidebar__item--actif:hover {
  background: rgb(255 255 255 / 16%);
  font-weight: 600;
}

.sidebar__icone {
  flex-shrink: 0;
}

.sidebar__label {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Atoms/Data display/Counter */
.sidebar__compteur {
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: var(--color-accent-300);
  color: #000;
  font-size: var(--size-caption);
  font-weight: 400;
  line-height: var(--line-height-caption);
}

/* Infobulle du menu replié (Nav Item, Collapsed). */
.sidebar__infobulle {
  display: none;
  position: absolute;
  top: 50%;
  left: calc(100% + 12px);
  z-index: 3;
  padding: 4px 8px;
  border-radius: var(--radius-sm);
  background: var(--color-neutrals-400);
  color: var(--color-neutrals-white);
  font-size: var(--size-caption);
  font-weight: 400;
  line-height: var(--line-height-caption);
  white-space: nowrap;
  transform: translateY(-50%);
  pointer-events: none;
}

.sidebar__infobulle::before {
  content: '';
  position: absolute;
  top: 50%;
  right: 100%;
  border: 5px solid transparent;
  border-right-color: var(--color-neutrals-400);
  transform: translateY(-50%);
}

.sidebar__utilisateur {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: var(--radius-md);
  background: rgb(255 255 255 / 10%);
}

.sidebar__avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 40px;
  height: 40px;
  box-sizing: border-box;
  border: 1px solid var(--color-neutrals-50);
  border-radius: var(--radius-full);
  background: var(--color-neutrals-white);
  color: var(--color-primary-300);
  font-weight: 600;
}

.sidebar__identite {
  flex: 1;
}

.sidebar__identite strong {
  font-weight: 600;
  line-height: var(--line-height-body);
}

.sidebar__deconnexion,
.sidebar__fermer {
  padding: 0;
  border: 0;
  border-radius: var(--radius-sm);
  background: none;
  cursor: pointer;
}

.sidebar__deconnexion {
  display: flex;
}

/* ---------- Menu replié : Sidebar Collapsed=true, 92 px (pas sur mobile) ---------- */
@media (width >= 768px) {
  .agl--reduit .sidebar {
    align-items: center;
    width: 92px;
  }

  .agl--reduit .sidebar__nom,
  .agl--reduit .sidebar__label,
  .agl--reduit .sidebar__compteur,
  .agl--reduit .sidebar__identite,
  .agl--reduit .sidebar__deconnexion {
    display: none;
  }

  .agl--reduit .sidebar__item {
    width: auto;
    height: 44px;
  }

  .agl--reduit .sidebar__item:hover .sidebar__infobulle,
  .agl--reduit .sidebar__item:focus-visible .sidebar__infobulle {
    display: block;
  }

  .agl--reduit .sidebar__nav,
  .agl--reduit .sidebar__bas {
    align-items: center;
  }

  .agl--reduit .sidebar__bas {
    align-self: stretch;
  }

  .agl--reduit .sidebar__utilisateur {
    justify-content: center;
    padding: 2px;
  }
}

.agl__voile {
  display: none;
}

/* ---------- Contenu ---------- */
.main {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 40px;
  min-width: 0;
  padding: 48px;
}

.barre-mobile {
  display: none;
}

.topbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

/* Molecules/Navigation/Breadcrumb */
.fil {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  margin: 0;
  padding: 0;
  list-style: none;
  color: var(--color-neutrals-200);
  line-height: var(--line-height-body);
}

.fil__niveau {
  display: flex;
  align-items: center;
}

.fil__courant {
  color: var(--color-neutrals-400);
  font-weight: 500;
}

.topbar__droite {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-left: auto;
}

/* Atoms/Data display/Badge, Status=Neutral */
.badge {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 12px;
  border-radius: var(--radius-full);
  background: rgb(209 213 219 / 60%);
  color: var(--color-neutrals-300);
  font-size: var(--size-overline);
  font-weight: 500;
  line-height: var(--line-height-overline);
  letter-spacing: 0.96px;
  text-transform: uppercase;
  white-space: nowrap;
}

/* Atoms/Actions/Icon Button */
.topbar__notifications {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: 1px solid var(--color-neutrals-50);
  border-radius: var(--radius-md);
  background: var(--color-neutrals-white);
  cursor: pointer;
}

.entete {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.entete__titre {
  margin: 0;
  font-size: var(--size-h1);
  font-weight: 600;
  line-height: var(--line-height-h1);
}

.entete__sous-titre {
  margin: 0;
  color: var(--color-neutrals-200);
  line-height: var(--line-height-body);
}

/* ---------- Tablette ---------- */
@media (width < 1024px) {
  .main {
    gap: 32px;
    padding: 32px 24px;
  }
}

/* ---------- Mobile ---------- */
@media (width < 768px) {
  /* Le menu complet s'ouvre par-dessus la page, quel que soit le choix « réduit ». */
  .sidebar {
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    z-index: 4;
    width: min(300px, 85vw);
    height: auto;
    overflow-y: auto;
    transform: translateX(-100%);
    transition: transform 0.25s ease;
  }

  .agl--mobile-ouvert .sidebar {
    transform: none;
  }

  .sidebar__fermer {
    display: flex;
  }

  .sidebar__bascule {
    display: none;
  }

  .agl__voile {
    display: block;
    position: fixed;
    inset: 0;
    z-index: 3;
    background: rgb(51 0 15 / 40%);
    opacity: 0;
    visibility: hidden;
    transition:
      opacity 0.25s ease,
      visibility 0.25s;
  }

  .agl--mobile-ouvert .agl__voile {
    opacity: 1;
    visibility: visible;
  }

  .main {
    gap: 16px;
    padding: 0 16px 24px;
  }

  /* Page Header Platform=Mobile : 56 px, titre centré. Le fil d'Ariane disparaît. */
  .barre-mobile {
    position: sticky;
    top: 0;
    z-index: 1;
    display: flex;
    align-items: center;
    gap: 4px;
    height: 56px;
    margin: 0 -8px;
    background: var(--color-neutrals-white);
  }

  .barre-mobile__bouton {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    width: 44px;
    height: 44px;
    padding: 0;
    border: 0;
    border-radius: var(--radius-md);
    background: var(--color-neutrals-white);
    cursor: pointer;
  }

  .barre-mobile__titre {
    flex: 1;
    min-width: 0;
    margin: 0;
    overflow: hidden;
    font-size: var(--size-h3);
    font-weight: 600;
    line-height: var(--line-height-h3);
    text-align: center;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .fil,
  .topbar__notifications,
  .entete__titre {
    display: none;
  }

  .topbar {
    justify-content: flex-start;
  }

  .topbar__droite {
    margin-left: 0;
  }

  .badge {
    white-space: normal;
  }

  .entete {
    order: -1;
    margin-top: 0;
  }

  .barre-mobile {
    order: -2;
  }
}
</style>

<style>
/* Menu mobile ouvert : la page derrière ne défile pas. */
body.agl-menu-ouvert {
  overflow: hidden;
}
</style>
