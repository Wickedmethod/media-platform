import { createApp } from 'vue'
import { createPinia } from 'pinia'
import TvApp from './TvApp.vue'
import '@shared/styles/main.css'

const app = createApp(TvApp)

app.use(createPinia())

app.mount('#tv-app')
