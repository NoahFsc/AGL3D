/**
 * Modèle de données d'un état des lieux (EDL).
 * Miroir de public/data/<dossier>/<dossier>.json : garder les deux synchronisés.
 */

/** Clé de pièce, ex. `salon`. */
export type PieceKey = string

/**
 * Clé stable d'élément, partagée avec Unity (`InspectableElement.key`).
 * Format : `<piece>.<element>` en kebab-case, ex. `salon.mur-ouest`.
 */
export type ElementKey = `${PieceKey}.${string}`

export type CategorieElement = 'mur' | 'sol' | 'ouvrant' | 'equipement' | 'compteur' | 'rangement'

/** État constaté d'un élément. Couleurs du repère : vert, orange, rouge. */
export type Etat = 'bon' | 'usage' | 'mauvais'

/** États du meilleur au pire, pour comparer l'entrée et la sortie. */
export const ETATS: readonly Etat[] = ['bon', 'usage', 'mauvais']

export type TypeEdl = 'entree' | 'sortie'

export interface Logement {
  id: string
  libelle: string
  adresse: string
  surfaceM2: number
  meuble: boolean
}

export interface Piece {
  key: PieceKey
  label: string
}

export interface Element {
  key: ElementKey
  pieceKey: PieceKey
  label: string
  categorie: CategorieElement
}

/** Index d'un compteur. */
export interface Releve {
  valeur: number
  unite: string
}

export interface Constat {
  elementKey: ElementKey
  etat: Etat
  commentaire: string
  /** Chemin relatif au dossier du logement, ex. `photos/entree-salon-mur-ouest.webp`. */
  photo: string | null
  /** Uniquement pour les compteurs. */
  releve?: Releve
}

export interface Edl {
  type: TypeEdl
  /** Date ISO `YYYY-MM-DD`. */
  date: string
  constats: Constat[]
}

/** Données de référence d'un logement : contenu de `<dossier>.json`. */
export interface Dossier {
  schemaVersion: 2
  logement: Logement
  pieces: Piece[]
  elements: Element[]
  /** Jeu de démo chargé à la demande, par exemple pour la soutenance. */
  demo: { edls: Edl[] }
}
