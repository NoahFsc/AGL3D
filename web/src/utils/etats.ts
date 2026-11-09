/** Libellés et couleurs des badges d'état, partagés par la liste et la fiche de comparaison. */
import type { Etat } from '@/types/edl'

export const BADGES_ETAT: Record<Etat, { label: string; classes: string }> = {
  bon: { label: 'Bon', classes: 'bg-bon-100 text-bon-500' },
  usage: { label: 'Usage', classes: 'bg-usage-100 text-usage-500' },
  mauvais: { label: 'Mauvais', classes: 'bg-mauvais-100 text-mauvais-500' },
}

/** Nombre au format français, jusqu'à 3 décimales. */
export function formaterNombre(valeur: number): string {
  return valeur.toLocaleString('fr-FR', { maximumFractionDigits: 3 })
}
