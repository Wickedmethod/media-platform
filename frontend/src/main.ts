import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import App from './App.vue'
import router from './router'
import { setupGlobalErrorHandler } from './plugins/error-handler'
import { vueQueryOptions } from './plugins/query-client'
import '@shared/styles/main.css'

const app = createApp(App)

app.use(createPinia())
app.use(VueQueryPlugin, vueQueryOptions)
app.use(router)

setupGlobalErrorHandler(app)

app.mount('#app')
