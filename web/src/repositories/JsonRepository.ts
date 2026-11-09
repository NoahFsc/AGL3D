import type { Constat, Dossier, Edl, TypeEdl } from '@/types/edl'
import type { EdlRepository } from './EdlRepository'
import { validerDossier, validerEdls } from './validation'

const STORAGE_PREFIX = 'agl3d:edls:'

/**
 * Implémentation sans serveur.
 * - Données de référence : fichier statique `<baseUrl>/<dossierId>/<dossierId>.json`.
 * - Constats saisis : `localStorage`, clé `agl3d:edls:<dossierId>`.
 * Les deux sont vérifiés à la lecture (`validation.ts`).
 */
export class JsonRepository implements EdlRepository {
  private readonly cache = new Map<string, Promise<Dossier>>()
  /** Copie en mémoire, utilisée si le navigateur refuse le `localStorage`. */
  private readonly memory = new Map<string, Edl[]>()

  constructor(
    private readonly baseUrl = `${import.meta.env.BASE_URL}data`,
    private readonly storage: Storage | null = safeLocalStorage(),
  ) {}

  getDossier(dossierId: string): Promise<Dossier> {
    let pending = this.cache.get(dossierId)
    if (!pending) {
      pending = this.fetchDossier(dossierId)
      // On ne garde pas en cache un échec, pour permettre un nouvel essai.
      pending.catch(() => this.cache.delete(dossierId))
      this.cache.set(dossierId, pending)
    }
    return pending
  }

  async listEdls(dossierId: string): Promise<Edl[]> {
    return this.read(dossierId)
  }

  async saveConstat(dossierId: string, type: TypeEdl, constat: Constat): Promise<Edl> {
    const edls = this.read(dossierId)
    let edl = edls.find((e) => e.type === type)
    if (!edl) {
      edl = { type, date: new Date().toISOString().slice(0, 10), constats: [] }
      edls.push(edl)
    }
    edl.constats = [...edl.constats.filter((c) => c.elementKey !== constat.elementKey), constat]
    this.write(dossierId, edls)
    return edl
  }

  async loadDemo(dossierId: string): Promise<Edl[]> {
    const dossier = await this.getDossier(dossierId)
    const edls = structuredClone(dossier.demo.edls)
    this.write(dossierId, edls)
    return edls
  }

  async reset(dossierId: string): Promise<void> {
    this.memory.delete(dossierId)
    try {
      this.storage?.removeItem(STORAGE_PREFIX + dossierId)
    } catch {
      // Rien à faire : la copie en mémoire est déjà vidée.
    }
  }

  photoUrl(dossierId: string, photo: string): string {
    return `${this.baseUrl}/${encodeURIComponent(dossierId)}/${photo}`
  }

  private async fetchDossier(dossierId: string): Promise<Dossier> {
    const id = encodeURIComponent(dossierId)
    const url = `${this.baseUrl}/${id}/${id}.json`
    const response = await fetch(url)
    if (!response.ok) {
      throw new Error(`Dossier "${dossierId}" introuvable (${response.status} sur ${url})`)
    }
    let data: unknown
    try {
      data = await response.json()
    } catch (e) {
      throw new Error(`Dossier "${dossierId}" : ${url} n'est pas du JSON valide`, { cause: e })
    }
    return validerDossier(data, url)
  }

  private read(dossierId: string): Edl[] {
    const key = STORAGE_PREFIX + dossierId
    let raw: string | null | undefined
    try {
      raw = this.storage?.getItem(key)
    } catch {
      // Stockage indisponible : on se rabat sur la mémoire.
    }
    if (raw) {
      try {
        return validerEdls(JSON.parse(raw), `localStorage "${key}"`)
      } catch (e) {
        // Contenu abîmé ou d'une ancienne version : on l'ignore, le prochain
        // enregistrement le remplacera.
        console.warn('[JsonRepository] Constats du navigateur ignorés', e)
      }
    }
    return structuredClone(this.memory.get(dossierId) ?? [])
  }

  private write(dossierId: string, edls: Edl[]): void {
    this.memory.set(dossierId, structuredClone(edls))
    try {
      this.storage?.setItem(STORAGE_PREFIX + dossierId, JSON.stringify(edls))
    } catch (e) {
      // Navigation privée ou quota dépassé : les constats restent en mémoire pour la session.
      console.warn('[JsonRepository] Constats non enregistrés dans le navigateur', e)
    }
  }
}

function safeLocalStorage(): Storage | null {
  try {
    return window.localStorage
  } catch {
    return null
  }
}
