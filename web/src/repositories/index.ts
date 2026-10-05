import type { EdlRepository } from './EdlRepository'
import { JsonRepository } from './JsonRepository'

export type { EdlRepository } from './EdlRepository'
export { JsonRepository } from './JsonRepository'

/** Instance utilisée par l'application. Point unique à changer pour l'intégration Laravel. */
export const edlRepository: EdlRepository = new JsonRepository()
