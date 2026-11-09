<script setup lang="ts">
import { computed } from 'vue'
import type { CibleCamera } from '@/types/bridge'
import type { Piece } from '@/types/edl'

const props = defineProps<{
  pieces: Piece[]
  courante: CibleCamera | null
}>()

const emit = defineEmits<{ focus: [room: CibleCamera] }>()

const boutons = computed<{ key: CibleCamera; label: string }[]>(() => [
  { key: 'overview', label: "Vue d'ensemble" },
  ...props.pieces,
])

function estCourante(key: CibleCamera): boolean {
  return key === (props.courante ?? 'overview')
}
</script>

<template>
  <div class="pointer-events-none absolute inset-x-0 bottom-0 p-4 max-md:p-3">
    <nav class="pointer-events-auto flex flex-wrap gap-3 max-md:gap-2" aria-label="Pièces">
      <button
        v-for="bouton in boutons"
        :key="bouton.key"
        type="button"
        class="inline-flex h-9 cursor-pointer items-center justify-center rounded-sm border-2 border-primary-300 px-4 whitespace-nowrap shadow-raised focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-300 max-md:h-12 max-md:flex-1 max-md:px-3"
        :class="estCourante(bouton.key) ? 'bg-primary-300 text-white' : 'bg-white text-primary-300'"
        :aria-pressed="estCourante(bouton.key)"
        @click="emit('focus', bouton.key)"
      >
        {{ bouton.label }}
      </button>
    </nav>
  </div>
</template>
