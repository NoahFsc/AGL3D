<script setup lang="ts">
import { ref } from 'vue'
import { useUnity } from '@/composables/useUnity'
import type { UnityToVueMessage } from '@/types/bridge'

const emit = defineEmits<{ message: [message: UnityToVueMessage] }>()

const canvas = ref<HTMLCanvasElement | null>(null)

const { progress, isLoaded, error, send } = useUnity(canvas, {
  onMessage: (message) => emit('message', message),
})

defineExpose({ send })
</script>

<template>
  <div class="viewer">
    <!-- Le canvas doit exister dès le montage : useUnity le lit dans onMounted.
         Unity exige un id : il retrouve le canvas avec querySelector('#<id>'). -->
    <canvas id="unity-canvas" ref="canvas" class="viewer__canvas" tabindex="-1"></canvas>

    <div v-if="error" class="viewer__overlay viewer__overlay--error">
      <p>Impossible de charger la 3D.</p>
      <p class="viewer__detail">{{ error.message }}</p>
      <p class="viewer__detail">
        Vérifiez que le build Unity est bien dans <code>web/public/unity/</code>.
      </p>
    </div>
    <div v-else-if="!isLoaded" class="viewer__overlay">
      Chargement de la 3D… {{ Math.round(progress * 100) }} %
    </div>
  </div>
</template>

<style scoped>
.viewer {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  background: #1e1e1e;
}

.viewer__canvas {
  display: block;
  width: 100%;
  height: 100%;
}

.viewer__overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.25rem;
  color: #eee;
  text-align: center;
  padding: 1rem;
}

.viewer__overlay--error {
  color: #ffb4a8;
}

.viewer__detail {
  font-size: 0.85rem;
  opacity: 0.8;
}
</style>
