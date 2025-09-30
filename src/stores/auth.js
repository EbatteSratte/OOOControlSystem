import { defineStore } from 'pinia'
import api from '../api'
export const useAuthStore = defineStore('auth', {
  state: () => ({ token: localStorage.getItem('token') || '', user: JSON.parse(localStorage.getItem('user')||'null') }),
  getters: { role: s => s.user?.role ?? null },
  actions: {
    setToken(t){ this.token=t; localStorage.setItem('token', t) },
    setUser(u){ this.user=u; localStorage.setItem('user', JSON.stringify(u)) },
    async register({ fullName, email, password }){
      const { data } = await api.post('/api/auth/register', { FullName: fullName, Email: email, Password: password })
      this.setToken(data?.token || data?.Token); await this.fetchProfile()
    },
    async login({ email, password }){
      const { data } = await api.post('/api/auth/login', { Email: email, Password: password })
      this.setToken(data?.token || data?.Token); await this.fetchProfile()
    },
    async fetchProfile(){
      const { data } = await api.get('/api/users/get-profile')
      this.setUser({ id: data.id, email: data.email, fullName: data.fullName, role: data.role, createdAt: data.createdAt, ...data })
      return this.user
    },
    logout(){ this.token=''; this.user=null; localStorage.removeItem('token'); localStorage.removeItem('user') }
  }
})
