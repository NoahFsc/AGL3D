/**
 * Contrat de messages Vue ↔ Unity.
 * Référence : docs/contrat-messages.md — toute modification passe par ce document
 * et par `unity/Assets/Scripts/Bridge/BridgeMessages.cs`.
 */
import type { ElementKey, PieceKey, StatutComparaison } from './edl'

/** Mode d'affichage de la maquette. */
export type ModeAffichage = 'entree' | 'sortie' | 'comparer'

/** Pièce ciblée par la caméra ; `overview` = vue d'ensemble de l'appartement. */
export type CibleCamera = PieceKey | 'overview'

/** Statut d'un point tel que coloré par Unity. En mode entrée/sortie, `neutre`. */
export type StatutPoint = StatutComparaison | 'neutre'

export interface PointPayload {
  key: ElementKey
  numero: number
  label: string
  statut: StatutPoint
}

/* ----------------------------- Vue → Unity ----------------------------- */

export interface VueToUnityPayloads {
  init: { dossierId: string; mode: ModeAffichage }
  setPoints: { points: PointPayload[] }
  focusRoom: { room: CibleCamera }
  select: { key: ElementKey | null }
  setContrast: { enabled: boolean }
}

export type VueToUnityType = keyof VueToUnityPayloads

/* ----------------------------- Unity → Vue ----------------------------- */

export interface UnityToVuePayloads {
  ready: { keys: ElementKey[] }
  /** `x`, `y` normalisés [0..1] dans le canvas, origine en haut à gauche. */
  pointSelected: { key: ElementKey; x: number; y: number }
  roomChanged: { room: CibleCamera }
}

export type UnityToVueType = keyof UnityToVuePayloads

export type UnityToVueMessage = {
  [T in UnityToVueType]: { type: T; payload: UnityToVuePayloads[T] }
}[UnityToVueType]

/** Nom de l'événement DOM émis par `AGL3DBridge.jslib` sur le canvas. */
export const UNITY_EVENT = 'agl3d'
