import axios from 'axios'
import { useAuthStore } from './stores/auth'

const base = import.meta.env.VITE_API_BASE || '' // если пусто — работаем через proxy
const api = axios.create({ baseURL: base, timeout: 10000 })

api.interceptors.request.use(cfg => {
  const auth = useAuthStore()
  if (auth?.token) cfg.headers.Authorization = `Bearer ${auth.token}`
  return cfg
})

api.interceptors.response.use(
  r => r,
  err => {
    try {
      const auth = useAuthStore()
      const url = (err && err.config && err.config.url) ? err.config.url : ''
      const status = err && err.response && err.response.status
      // Логаем, чтобы увидеть, что именно дало 401
      if (status === 401) {
        console.warn('[API 401]', url)
      }
      // Авто-выход — ТОЛЬКО для get-profile и auth-эндпоинтов
      if (status === 401 && (/\/api\/users\/get-profile$/.test(url) || /\/api\/auth\//.test(url))) {
        auth && auth.logout && auth.logout()
      }
    } catch(e) {
      // no-op
    }
    return Promise.reject(err)
  }
)

export default api
