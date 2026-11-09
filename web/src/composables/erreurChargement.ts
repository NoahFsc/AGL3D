/**
 * Erreurs de chargement du build Unity, classées pour afficher un message utile
 * (tâche B8). Le message technique d'origine reste dans `message`.
 */
export type TypeErreurChargement =
  /** Le navigateur ne gère pas WebGL : réessayer ne sert à rien. */
  | 'webgl'
  /** Fichier du build absent (404) : build pas fait ou mal placé. */
  | 'introuvable'
  /** Téléchargement coupé, serveur injoignable. */
  | 'reseau'
  /** Le build a démarré mais n'a jamais envoyé `ready`. */
  | 'muet'
  /** Tout le reste : fichier abîmé, erreur au démarrage de Unity. */
  | 'inconnu'

export class ErreurChargement extends Error {
  constructor(
    readonly type: TypeErreurChargement,
    message: string,
    options?: { cause?: unknown },
  ) {
    super(message, options)
    this.name = 'ErreurChargement'
  }
}

/** Vrai si le navigateur peut créer un contexte WebGL. */
export function webglDisponible(): boolean {
  try {
    const canvas = document.createElement('canvas')
    return !!(canvas.getContext('webgl2') ?? canvas.getContext('webgl'))
  } catch {
    return false
  }
}

/**
 * Classe une erreur levée par `createUnityInstance` ou par le chargement du loader.
 * Unity ne donne pas de code d'erreur : on lit son message.
 */
export function classerErreur(erreur: unknown): ErreurChargement {
  if (erreur instanceof ErreurChargement) return erreur
  const message = erreur instanceof Error ? erreur.message : String(erreur)
  const texte = message.toLowerCase()

  let type: TypeErreurChargement = 'inconnu'
  if (texte.includes('webgl')) type = 'webgl'
  else if (/\b404\b|not found|introuvable/.test(texte)) type = 'introuvable'
  else if (/failed to fetch|networkerror|network error|load failed|err_|aborterror/.test(texte)) {
    type = 'reseau'
  }
  return new ErreurChargement(type, message, { cause: erreur })
}

/**
 * Le loader est chargé par une balise `<script>`, qui ne dit pas pourquoi elle échoue.
 * On redemande le fichier pour distinguer un fichier absent d'un problème de réseau.
 */
export async function diagnostiquerScript(url: string): Promise<ErreurChargement> {
  try {
    const reponse = await fetch(url, { method: 'HEAD', cache: 'no-store' })
    // Un serveur d'application (Vite en dev, par exemple) renvoie sa page HTML à la
    // place d'un fichier absent : c'est aussi un fichier introuvable.
    const html = reponse.headers.get('content-type')?.includes('text/html') ?? false
    if (reponse.status === 404 || html) {
      return new ErreurChargement(
        'introuvable',
        `Loader Unity introuvable (${reponse.status}${html ? ', page HTML reçue' : ''}) : ${url}`,
      )
    }
    return new ErreurChargement('inconnu', `Loader Unity illisible (${reponse.status}) : ${url}`)
  } catch (e) {
    return new ErreurChargement('reseau', `Loader Unity injoignable : ${url}`, { cause: e })
  }
}
