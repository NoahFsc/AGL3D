/**
 * Vérification des données lues par `JsonRepository` : le fichier `<dossier>.json` et les
 * constats gardés dans le navigateur. Le JSON vient de l'extérieur du code, TypeScript ne
 * le vérifie pas : on contrôle sa forme avant de lui faire confiance.
 */
import { ETATS, type Dossier, type Edl } from '@/types/edl'

const CATEGORIES = ['mur', 'sol', 'ouvrant', 'equipement', 'compteur', 'rangement']
const KEBAB = /^[a-z0-9]+(-[a-z0-9]+)*$/
const CLE_ELEMENT = /^([a-z0-9]+(?:-[a-z0-9]+)*)\.[a-z0-9]+(-[a-z0-9]+)*$/
const DATE_ISO = /^\d{4}-\d{2}-\d{2}$/

/** Données invalides : `problemes` liste chaque erreur avec son chemin dans le JSON. */
export class DonneesInvalides extends Error {
  constructor(
    source: string,
    readonly problemes: string[],
  ) {
    const apercu = problemes.slice(0, 5).join('\n- ')
    const reste = problemes.length > 5 ? `\n… et ${problemes.length - 5} autre(s)` : ''
    super(`${source} invalide :\n- ${apercu}${reste}`)
    this.name = 'DonneesInvalides'
  }
}

type Objet = Record<string, unknown>

function estObjet(v: unknown): v is Objet {
  return typeof v === 'object' && v !== null && !Array.isArray(v)
}

/** Accumule les erreurs au lieu de s'arrêter à la première. */
class Verif {
  readonly problemes: string[] = []

  ajouter(chemin: string, message: string): void {
    this.problemes.push(`${chemin} : ${message}`)
  }

  objet(v: unknown, chemin: string): v is Objet {
    if (estObjet(v)) return true
    this.ajouter(chemin, 'objet attendu')
    return false
  }

  tableau(v: unknown, chemin: string): v is unknown[] {
    if (Array.isArray(v)) return true
    this.ajouter(chemin, 'tableau attendu')
    return false
  }

  texte(v: unknown, chemin: string, nonVide = true): v is string {
    if (typeof v === 'string' && (!nonVide || v.trim() !== '')) return true
    this.ajouter(chemin, nonVide ? 'texte non vide attendu' : 'texte attendu')
    return false
  }

  nombre(v: unknown, chemin: string): v is number {
    if (typeof v === 'number' && Number.isFinite(v)) return true
    this.ajouter(chemin, 'nombre attendu')
    return false
  }

  parmi(v: unknown, valeurs: readonly string[], chemin: string): boolean {
    if (typeof v === 'string' && valeurs.includes(v)) return true
    this.ajouter(chemin, `${JSON.stringify(v)} au lieu de ${valeurs.join(', ')}`)
    return false
  }
}

/**
 * Vérifie le contenu de `<dossier>.json` et le renvoie typé.
 * Lève `DonneesInvalides` avec la liste des problèmes trouvés.
 */
export function validerDossier(data: unknown, source: string): Dossier {
  const v = new Verif()
  if (!v.objet(data, 'racine')) throw new DonneesInvalides(source, v.problemes)

  if (data.schemaVersion !== 2) {
    v.ajouter('schemaVersion', `2 attendu, ${JSON.stringify(data.schemaVersion)} trouvé`)
  }

  const logement = data.logement
  if (v.objet(logement, 'logement')) {
    v.texte(logement.id, 'logement.id')
    v.texte(logement.libelle, 'logement.libelle')
    v.texte(logement.adresse, 'logement.adresse')
    v.nombre(logement.surfaceM2, 'logement.surfaceM2')
    if (typeof logement.meuble !== 'boolean') v.ajouter('logement.meuble', 'booléen attendu')
  }

  const pieces = new Set<string>()
  if (v.tableau(data.pieces, 'pieces')) {
    data.pieces.forEach((p, i) => {
      const chemin = `pieces[${i}]`
      if (!v.objet(p, chemin)) return
      if (v.texte(p.key, `${chemin}.key`)) {
        if (!KEBAB.test(p.key)) v.ajouter(`${chemin}.key`, `"${p.key}" n'est pas en kebab-case`)
        if (pieces.has(p.key)) v.ajouter(`${chemin}.key`, `"${p.key}" en double`)
        pieces.add(p.key)
      }
      v.texte(p.label, `${chemin}.label`)
    })
  }

  const elements = new Set<string>()
  if (v.tableau(data.elements, 'elements')) {
    data.elements.forEach((e, i) => {
      const chemin = `elements[${i}]`
      if (!v.objet(e, chemin)) return
      if (v.texte(e.key, `${chemin}.key`)) {
        const format = CLE_ELEMENT.exec(e.key)
        if (!format) {
          v.ajouter(`${chemin}.key`, `"${e.key}" ne suit pas <piece>.<element> en kebab-case`)
        } else if (format[1] !== e.pieceKey) {
          v.ajouter(`${chemin}.key`, `"${e.key}" ne commence pas par sa pièce "${e.pieceKey}"`)
        }
        if (elements.has(e.key)) v.ajouter(`${chemin}.key`, `"${e.key}" en double`)
        elements.add(e.key)
      }
      if (v.texte(e.pieceKey, `${chemin}.pieceKey`) && !pieces.has(e.pieceKey)) {
        v.ajouter(`${chemin}.pieceKey`, `pièce "${e.pieceKey}" absente de pieces`)
      }
      v.texte(e.label, `${chemin}.label`)
      v.parmi(e.categorie, CATEGORIES, `${chemin}.categorie`)
    })
  }

  if (v.objet(data.demo, 'demo')) verifierEdls(v, data.demo.edls, 'demo.edls', elements)

  if (v.problemes.length > 0) throw new DonneesInvalides(source, v.problemes)
  return data as unknown as Dossier
}

/**
 * Vérifie des états des lieux, par exemple ceux gardés dans le navigateur.
 * `elements` : clés connues du dossier, pour refuser un constat sur un élément inconnu.
 */
export function validerEdls(data: unknown, source: string, elements?: Iterable<string>): Edl[] {
  const v = new Verif()
  verifierEdls(v, data, 'edls', elements ? new Set(elements) : undefined)
  if (v.problemes.length > 0) throw new DonneesInvalides(source, v.problemes)
  return data as Edl[]
}

function verifierEdls(v: Verif, data: unknown, chemin: string, elements?: Set<string>): void {
  if (!v.tableau(data, chemin)) return
  const types = new Set<string>()
  data.forEach((edl, i) => {
    const c = `${chemin}[${i}]`
    if (!v.objet(edl, c)) return
    if (v.parmi(edl.type, ['entree', 'sortie'], `${c}.type`)) {
      if (types.has(edl.type as string)) v.ajouter(`${c}.type`, `"${edl.type}" en double`)
      types.add(edl.type as string)
    }
    if (v.texte(edl.date, `${c}.date`) && !DATE_ISO.test(edl.date)) {
      v.ajouter(`${c}.date`, `"${edl.date}" n'est pas au format AAAA-MM-JJ`)
    }
    if (!v.tableau(edl.constats, `${c}.constats`)) return
    const vus = new Set<string>()
    edl.constats.forEach((constat, j) => verifierConstat(v, constat, `${c}.constats[${j}]`, vus, elements))
  })
}

function verifierConstat(
  v: Verif,
  constat: unknown,
  chemin: string,
  vus: Set<string>,
  elements?: Set<string>,
): void {
  if (!v.objet(constat, chemin)) return
  if (v.texte(constat.elementKey, `${chemin}.elementKey`)) {
    if (elements && !elements.has(constat.elementKey)) {
      v.ajouter(`${chemin}.elementKey`, `élément "${constat.elementKey}" absent de elements`)
    }
    if (vus.has(constat.elementKey)) v.ajouter(`${chemin}.elementKey`, `"${constat.elementKey}" en double`)
    vus.add(constat.elementKey)
  }
  v.parmi(constat.etat, ETATS, `${chemin}.etat`)
  v.texte(constat.commentaire, `${chemin}.commentaire`, false)
  if (constat.photo !== null) v.texte(constat.photo, `${chemin}.photo`)
  if (constat.releve !== undefined && v.objet(constat.releve, `${chemin}.releve`)) {
    v.nombre(constat.releve.valeur, `${chemin}.releve.valeur`)
    v.texte(constat.releve.unite, `${chemin}.releve.unite`)
  }
}
