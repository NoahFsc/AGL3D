import type { DossierEdl } from '@/types/edl'
import type { EdlRepository } from './EdlRepository'

/**
 * Implémentation sur fichiers statiques :
 * `<baseUrl>/<dossierId>/<dossierId>.json` et `<baseUrl>/<dossierId>/photos/…`.
 */
export class JsonRepository implements EdlRepository {
  private readonly cache = new Map<string, Promise<DossierEdl>>()

  constructor(private readonly baseUrl = `${import.meta.env.BASE_URL}data`) {}

  getDossier(dossierId: string): Promise<DossierEdl> {
    let pending = this.cache.get(dossierId)
    if (!pending) {
      pending = this.fetchDossier(dossierId)
      // On ne garde pas en cache un échec, pour permettre un nouvel essai.
      pending.catch(() => this.cache.delete(dossierId))
      this.cache.set(dossierId, pending)
    }
    return pending
  }

  photoUrl(dossierId: string, photo: string): string {
    return `${this.baseUrl}/${encodeURIComponent(dossierId)}/${photo}`
  }

  private async fetchDossier(dossierId: string): Promise<DossierEdl> {
    const url = `${this.baseUrl}/${encodeURIComponent(dossierId)}/${encodeURIComponent(dossierId)}.json`
    const response = await fetch(url)
    if (!response.ok) {
      throw new Error(`Dossier « ${dossierId} » introuvable (${response.status} sur ${url})`)
    }
    // TODO(lot B) : valider le schéma (schemaVersion, clés d'éléments) avant de faire confiance au JSON.
    return (await response.json()) as DossierEdl
  }
}
