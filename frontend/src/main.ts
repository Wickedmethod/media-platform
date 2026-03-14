import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import App from './App.vue'
import router from './router'
import { setupGlobalErrorHandler } from './plugins/error-handler'
import { vueQueryOptions } from './plugins/query-client'
import { initAuth } from './plugins/auth'
import '@shared/styles/main.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(VueQueryPlugin, vueQueryOptions)
app.use(router)

setupGlobalErrorHandler(app)

initAuth()
  .then(() => app.mount('#app'))
  .catch((err) => {
    console.error('[app] Auth initialization failed:', err)
    app.mount('#app')
  })
