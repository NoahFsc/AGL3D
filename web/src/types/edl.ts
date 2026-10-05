/**
 * Modèle de données d'un état des lieux (EDL).
 * Miroir de public/data/<dossier>/<dossier>.json — garder les deux synchronisés.
 */

/** Clé de pièce, ex. `salon`. */
export type PieceKey = string

/**
 * Clé stable d'élément, partagée avec Unity (`InspectableElement.key`).
 * Format : `<piece>.<element>` en kebab-case, ex. `salon.mur-ouest`.
 */
export type ElementKey = `${PieceKey}.${string}`

export type CategorieElement =
  'mur' | 'sol' | 'plafond' | 'ouvrant' | 'equipement' | 'compteur' | 'rangement' | 'mobilier'

/** État constaté d'un élément, du meilleur au pire. */
export type Etat = 'neuf' | 'bon' | 'moyen' | 'mauvais'

export type TypeEdl = 'entree' | 'sortie'

/** Résultat de la comparaison entrée → sortie pour un point. */
export type StatutComparaison = 'inchange' | 'degrade' | 'ameliore' | 'releve'

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

/** Point numéroté affiché sur la maquette (marqueur). */
export interface Point {
  numero: number
  elementKey: ElementKey
}

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
  id: string
  type: TypeEdl
  /** Date ISO `YYYY-MM-DD`. */
  date: string
  constats: Constat[]
}

/** Contenu complet d'un fichier `<dossier>.json`. */
export interface DossierEdl {
  schemaVersion: 1
  logement: Logement
  pieces: Piece[]
  elements: Element[]
  points: Point[]
  edls: Edl[]
}
