import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface NowPlaying {
  title: string
  url: string
  thumbnailUrl?: string
  duration?: number
  position?: number
  requestedBy?: string
}

export type PlayerState = 'idle' | 'playing' | 'paused' | 'buffering' | 'error'

export const usePlayerStore = defineStore('player', () => {
  const state = ref<PlayerState>('idle')
  const nowPlaying = ref<NowPlaying | null>(null)
  const errorMessage = ref<string | null>(null)

  function updateFromSSE(event: string, data: unknown) {
    switch (event) {
      case 'player-state-changed': {
        const d = data as { state: PlayerState }
        state.value = d.state
        break
      }
      case 'now-playing-changed': {
        nowPlaying.value = data as NowPlaying | null
        break
      }
      case 'playback-error': {
        const d = data as { message: string }
        state.value = 'error'
        errorMessage.value = d.message
        break
      }
    }
  }

  function reset() {
    state.value = 'idle'
    nowPlaying.value = null
    errorMessage.value = null
  }

  return {
    state,
    nowPlaying,
    errorMessage,
    updateFromSSE,
    reset,
  }
})
