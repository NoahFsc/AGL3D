/** Typage minimal de l'API exposée par `<build>.loader.js` (Unity 6 Web). */

export interface UnityConfig {
  dataUrl: string
  frameworkUrl: string
  codeUrl: string
  streamingAssetsUrl?: string
  companyName?: string
  productName?: string
  productVersion?: string
  /** Désactive la mise à l'échelle devicePixelRatio si défini. */
  devicePixelRatio?: number
  matchWebGLToCanvasSize?: boolean
  showBanner?: (message: string, type: 'error' | 'warning' | 'info') => void
}

export interface UnityInstance {
  SendMessage(gameObject: string, method: string, value?: string | number): void
  SetFullscreen(fullscreen: 0 | 1): void
  Quit(): Promise<void>
}

declare global {
  interface Window {
    createUnityInstance?: (
      canvas: HTMLCanvasElement,
      config: UnityConfig,
      onProgress?: (progress: number) => void,
    ) => Promise<UnityInstance>
  }
}
