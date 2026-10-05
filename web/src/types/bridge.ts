/**
 * Contrat de messages Vue <-> Unity.
 * Référence : docs/contrat-messages.md. Toute modification passe par ce document
 * et par `unity/Assets/Scripts/Bridge/BridgeMessages.cs`.
 */
import type { ElementKey, Etat, PieceKey } from './edl'

/** Mode d'affichage. `accueil` : la maquette tourne lentement, vue d'ensemble. */
export type ModeAffichage = 'accueil' | 'entree' | 'sortie' | 'comparer'

/** Pièce ciblée par la caméra ; `overview` = vue d'ensemble de l'appartement. */
export type CibleCamera = PieceKey | 'overview'

/** Repère affiché sur un élément noté. */
export interface PointPayload {
  key: ElementKey
  label: string
  /** Couleur du repère : bon = vert, usage = orange, mauvais = rouge. */
  etat: Etat
  /** En mode Comparer, l'élément passe progressivement au rouge. */
  degrade: boolean
}

/* ----------------------------- Vue → Unity ----------------------------- */

export interface VueToUnityPayloads {
  init: { dossierId: string; mode: ModeAffichage }
  setPoints: { points: PointPayload[] }
  focusRoom: { room: CibleCamera }
  focusElement: { key: ElementKey }
  select: { key: ElementKey | null }
}

export type VueToUnityType = keyof VueToUnityPayloads

/* ----------------------------- Unity → Vue ----------------------------- */

export interface UnityToVuePayloads {
  ready: { keys: ElementKey[] }
  /** `x`, `y` normalisés [0..1] dans le canvas, origine en haut à gauche. */
  pointSelected: { key: ElementKey; x: number; y: number }
  /** Clic dans le vide : Vue ferme la fiche. */
  deselected: Record<string, never>
  roomChanged: { room: CibleCamera }
}

export type UnityToVueType = keyof UnityToVuePayloads

export type UnityToVueMessage = {
  [T in UnityToVueType]: { type: T; payload: UnityToVuePayloads[T] }
}[UnityToVueType]

/** Nom de l'événement DOM émis par `AGL3DBridge.jslib` sur le canvas. */
export const UNITY_EVENT = 'agl3d'
