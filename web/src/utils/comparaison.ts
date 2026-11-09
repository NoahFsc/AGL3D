/**
 * Comparaison de l'entrée et de la sortie (C5) : trouve les éléments qui se sont dégradés.
 *
 * Un élément est dégradé quand son état de sortie est pire que son état d'entrée, dans
 * l'ordre de `ETATS` (bon, puis usage, puis mauvais). Un élément noté d'un seul côté ne se
 * compare pas : il n'est pas dégradé. Les compteurs suivent la même règle ; leur
 * consommation se lit à part, dans leur fiche.
 */
import { ETATS, type Constat, type Edl, type ElementKey, type Etat } from '@/types/edl'

export interface Degradation {
  elementKey: ElementKey
  /** État à l'entrée. */
  avant: Etat
  /** État à la sortie, pire que `avant`. */
  apres: Etat
  /** Nombre de crans perdus : 1 (bon → usage, usage → mauvais) ou 2 (bon → mauvais). */
  ecart: number
  constatEntree: Constat
  constatSortie: Constat
}

/** Rang d'un état : 0 pour bon, 1 pour usage, 2 pour mauvais. */
function rang(etat: Etat): number {
  return ETATS.indexOf(etat)
}

/**
 * Éléments dégradés entre `entree` et `sortie`, du plus dégradé au moins dégradé ; à écart
 * égal, dans l'ordre des constats de sortie. Liste vide s'il manque l'un des deux.
 */
export function trouverDegradations(
  entree: Edl | null | undefined,
  sortie: Edl | null | undefined,
): Degradation[] {
  if (!entree || !sortie) return []
  const avant = new Map(entree.constats.map((c) => [c.elementKey, c]))

  const degradations: Degradation[] = []
  for (const constatSortie of sortie.constats) {
    const constatEntree = avant.get(constatSortie.elementKey)
    if (!constatEntree) continue
    const ecart = rang(constatSortie.etat) - rang(constatEntree.etat)
    if (ecart <= 0) continue
    degradations.push({
      elementKey: constatSortie.elementKey,
      avant: constatEntree.etat,
      apres: constatSortie.etat,
      ecart,
      constatEntree,
      constatSortie,
    })
  }
  // Le tri de JavaScript est stable : à écart égal, l'ordre de la sortie est gardé.
  return degradations.sort((a, b) => b.ecart - a.ecart)
}

/** Clés des éléments dégradés, pour savoir vite si un élément l'est. */
export function clesDegradees(
  entree: Edl | null | undefined,
  sortie: Edl | null | undefined,
): Set<ElementKey> {
  return new Set(trouverDegradations(entree, sortie).map((d) => d.elementKey))
}

/**
 * Évolution d'un élément entre l'entrée et la sortie : `degrade` (pire à la sortie),
 * `ameliore` (meilleur), `identique`, ou `incomplet` s'il n'est pas noté des deux côtés.
 */
export type Evolution = 'degrade' | 'ameliore' | 'identique' | 'incomplet'

export interface ComparaisonElement {
  avant: Constat | null
  apres: Constat | null
  evolution: Evolution
  /**
   * Consommation d'un compteur entre les deux relevés (index de sortie moins index
   * d'entrée), ou null s'il manque un relevé.
   */
  consommation: number | null
}

/** Compare un élément entre `entree` et `sortie`. */
export function comparerElement(
  entree: Edl | null | undefined,
  sortie: Edl | null | undefined,
  key: ElementKey,
): ComparaisonElement {
  const avant = entree?.constats.find((c) => c.elementKey === key) ?? null
  const apres = sortie?.constats.find((c) => c.elementKey === key) ?? null
  let evolution: Evolution = 'incomplet'
  if (avant && apres) {
    const ecart = rang(apres.etat) - rang(avant.etat)
    evolution = ecart > 0 ? 'degrade' : ecart < 0 ? 'ameliore' : 'identique'
  }
  const consommation =
    avant?.releve && apres?.releve ? apres.releve.valeur - avant.releve.valeur : null
  return { avant, apres, evolution, consommation }
}
