import { onBeforeUnmount, onMounted, ref, shallowRef, type Ref } from 'vue'
import {
  UNITY_EVENT,
  type UnityToVueMessage,
  type VueToUnityPayloads,
  type VueToUnityType,
} from '@/types/bridge'
import type { UnityInstance } from '@/types/unity-loader'
import {
  classerErreur,
  diagnostiquerScript,
  ErreurChargement,
  webglDisponible,
} from './erreurChargement'

export interface UseUnityOptions {
  /** Dossier du build WebGL servi par Vite. */
  buildUrl?: string
  /** Nom des fichiers du build (= nom du dossier de sortie choisi dans Unity). */
  buildName?: string
  /** Suffixe des fichiers compressés ; `.unityweb` avec Decompression Fallback. */
  compressedExt?: string
  /** Appelé pour chaque message Unity → Vue. */
  onMessage?: (message: UnityToVueMessage) => void
  /** Délai d'attente de `ready` après le démarrage du build, en millisecondes. */
  readyTimeout?: number
}

/** Nom du GameObject portant `WebBridge` dans la scène Unity. */
const BRIDGE_OBJECT = 'WebBridge'
const BRIDGE_METHOD = 'Receive'

/**
 * Charge le build Unity WebGL dans `canvas` et expose le pont de messages.
 * Les messages envoyés avant `ready` sont mis en attente, puis envoyés dans l'ordre
 * dès que Unity est chargé et a envoyé `ready` (docs/contrat-messages.md, Démarrage).
 */
export function useUnity(canvas: Ref<HTMLCanvasElement | null>, options: UseUnityOptions = {}) {
  const {
    buildUrl = `${import.meta.env.BASE_URL}unity/Build`,
    buildName = 'unity',
    compressedExt = '.unityweb',
    onMessage,
    readyTimeout = 30_000,
  } = options

  const instance = shallowRef<UnityInstance | null>(null)
  const progress = ref(0)
  const isLoaded = ref(false)
  const error = shallowRef<ErreurChargement | null>(null)
  /** Vrai quand Unity a envoyé `ready` : les messages partent directement. */
  const isReady = ref(false)

  /** Messages JSON en attente de `ready`, dans l'ordre d'envoi. */
  let pending: string[] = []
  /** Minuterie qui signale un build démarré mais muet (pas de `ready`). */
  let attenteReady: ReturnType<typeof setTimeout> | undefined

  function send<T extends VueToUnityType>(type: T, payload: VueToUnityPayloads[T]): void {
    const json = JSON.stringify({ type, payload })
    if (!instance.value || !isReady.value) {
      pending.push(json)
      return
    }
    instance.value.SendMessage(BRIDGE_OBJECT, BRIDGE_METHOD, json)
  }

  /**
   * Envoie les messages en attente. `ready` peut arriver avant la fin de
   * `createUnityInstance` : on attend donc d'avoir à la fois l'instance et `ready`.
   */
  function flush(): void {
    const current = instance.value
    if (!current || !isReady.value) return
    const queue = pending
    pending = []
    for (const json of queue) current.SendMessage(BRIDGE_OBJECT, BRIDGE_METHOD, json)
  }

  function handleEvent(event: Event): void {
    const message = (event as CustomEvent<UnityToVueMessage>).detail
    if (message.type === 'ready') {
      isReady.value = true
      clearTimeout(attenteReady)
      // Les messages en attente partent avant ceux que onMessage enverra en réponse.
      flush()
    }
    onMessage?.(message)
  }

  async function loadLoaderScript(): Promise<void> {
    if (window.createUnityInstance) return
    await new Promise<void>((resolve, reject) => {
      const script = document.createElement('script')
      script.src = `${buildUrl}/${buildName}.loader.js`
      script.async = true
      script.onload = () => resolve()
      script.onerror = () => {
        script.remove()
        diagnostiquerScript(script.src).then(reject, reject)
      }
      document.body.appendChild(script)
    })
  }

  onMounted(async () => {
    const el = canvas.value
    if (!el) return
    el.addEventListener(UNITY_EVENT, handleEvent)
    try {
      if (!webglDisponible()) {
        throw new ErreurChargement('webgl', 'Ce navigateur ne peut pas créer de contexte WebGL.')
      }
      await loadLoaderScript()
      if (!window.createUnityInstance) {
        throw new ErreurChargement('inconnu', 'createUnityInstance indisponible')
      }
      instance.value = await window.createUnityInstance(
        el,
        {
          dataUrl: `${buildUrl}/${buildName}.data${compressedExt}`,
          frameworkUrl: `${buildUrl}/${buildName}.framework.js${compressedExt}`,
          codeUrl: `${buildUrl}/${buildName}.wasm${compressedExt}`,
          companyName: 'AGL3D',
          productName: 'AGL3D',
          // Sans ce rappel, Unity affiche ses propres bandeaux par-dessus la page.
          showBanner: (message, type) => {
            if (type === 'error') console.error('[Unity]', message)
            else console.warn('[Unity]', message)
          },
        },
        (p) => (progress.value = p),
      )
      isLoaded.value = true
      flush()
      if (!isReady.value) {
        attenteReady = setTimeout(() => {
          if (isReady.value) return
          error.value = new ErreurChargement(
            'muet',
            `Unity n'a pas envoyé ready ${readyTimeout / 1000} s après son démarrage.`,
          )
        }, readyTimeout)
      }
    } catch (e) {
      error.value = classerErreur(e)
      console.error('[useUnity] Chargement impossible', e)
    }
  })

  onBeforeUnmount(async () => {
    clearTimeout(attenteReady)
    canvas.value?.removeEventListener(UNITY_EVENT, handleEvent)
    const current = instance.value
    instance.value = null
    isLoaded.value = false
    isReady.value = false
    pending = []
    await current?.Quit()
  })

  return { instance, progress, isLoaded, isReady, error, send }
}
