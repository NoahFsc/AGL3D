import type { Constat, Dossier, Edl, TypeEdl } from '@/types/edl'

/**
 * Accès aux données d'état des lieux.
 *
 * Seule porte d'entrée vers les données : les composants et composables ne doivent
 * jamais lire un fichier ni le stockage du navigateur directement. Aujourd'hui
 * `JsonRepository` lit des JSON statiques et garde les constats dans le navigateur.
 * Lors de l'intégration Laravel + Inertia, une autre implémentation prendra le relais
 * sans toucher au reste de l'app.
 */
export interface EdlRepository {
  /** Données de référence d'un logement : logement, pièces, éléments, jeu de démo. */
  getDossier(dossierId: string): Promise<Dossier>

  /** États des lieux enregistrés pour ce logement (0, 1 ou 2 : entrée et sortie). */
  listEdls(dossierId: string): Promise<Edl[]>

  /**
   * Enregistre un constat dans l'état des lieux `type`. Crée l'état des lieux à la date
   * du jour s'il n'existe pas. Remplace le constat existant pour le même élément.
   * Renvoie l'état des lieux à jour.
   */
  saveConstat(dossierId: string, type: TypeEdl, constat: Constat): Promise<Edl>

  /** Remplace les états des lieux enregistrés par ceux du jeu de démo. */
  loadDemo(dossierId: string): Promise<Edl[]>

  /** Supprime les états des lieux enregistrés pour ce logement. */
  reset(dossierId: string): Promise<void>

  /** URL absolue d'une photo référencée par un constat (`Constat.photo`). */
  photoUrl(dossierId: string, photo: string): string
}
