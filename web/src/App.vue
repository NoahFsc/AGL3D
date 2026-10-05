<script setup lang="ts">
import { ref } from 'vue'
import UnityViewer from '@/components/UnityViewer.vue'
import type { UnityToVueMessage } from '@/types/bridge'

// Page de test du pont Vue <-> Unity. L'UI finale arrive avec les lots A, B et C (docs/PLAN.md).

const viewer = ref<InstanceType<typeof UnityViewer> | null>(null)
const received = ref<UnityToVueMessage[]>([])

function onMessage(message: UnityToVueMessage) {
  console.info('[AGL3D] Unity -> Vue', message)
  received.value.unshift(message)

  if (message.type === 'ready') {
    viewer.value?.send('init', { dossierId: 'appart-demo', mode: 'accueil' })
  }
}
</script>

<template>
  <main class="page">
    <h1>AGL3D, test du pont Vue et Unity</h1>

    <UnityViewer ref="viewer" @message="onMessage" />

    <section>
      <h2>Messages reçus de Unity ({{ received.length }})</h2>
      <p v-if="received.length === 0">Aucun message pour l'instant.</p>
      <ul v-else class="log">
        <li v-for="(message, index) in received" :key="index">
          <code>{{ message.type }}</code> {{ JSON.stringify(message.payload) }}
        </li>
      </ul>
    </section>
  </main>
</template>

<style scoped>
.page {
  max-width: 960px;
  margin: 0 auto;
  padding: 1rem;
  font-family: system-ui, sans-serif;
}

.log {
  font-size: 0.85rem;
  padding-left: 1rem;
}
</style>
