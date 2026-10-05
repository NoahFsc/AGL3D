import type { DossierEdl } from '@/types/edl'

/**
 * Accès aux données d'état des lieux.
 *
 * Seule porte d'entrée vers les données : les composants et composables ne doivent
 * jamais faire de `fetch` directement. Aujourd'hui `JsonRepository` lit des JSON
 * statiques ; lors de l'intégration Laravel + Inertia, une autre implémentation
 * (props Inertia ou API) prendra le relais sans toucher au reste de l'app.
 */
export interface EdlRepository {
  /** Charge un dossier complet (logement, pièces, éléments, points, EDL). */
  getDossier(dossierId: string): Promise<DossierEdl>

  /** URL absolue d'une photo référencée par un constat (`Constat.photo`). */
  photoUrl(dossierId: string, photo: string): string
}
