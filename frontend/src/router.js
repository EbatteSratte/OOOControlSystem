import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from './stores/auth'

import DashboardLayout from './layouts/DashboardLayout.vue'
import AuthPage from './pages/AuthPage.vue'

// Manager pages
import MProfile from './pages/manager/Profile.vue'
import MUsers from './pages/manager/Users.vue'
import MProjects from './pages/manager/Projects.vue'
import MDefects from './pages/manager/Defects.vue'

// Engineer pages
import EProfile from './pages/engineer/Profile.vue'
import EProjects from './pages/engineer/Projects.vue'
import EDefects from './pages/engineer/Defects.vue'

// Customer pages
import CProfile from './pages/customer/Profile.vue'
import CProjects from './pages/customer/Projects.vue'
import CReports from './pages/customer/Reports.vue'

const routes = [
  { path: '/', name: 'auth', component: AuthPage },

  { path: '/manager', component: DashboardLayout, meta: { role: 'Manager' }, children: [
    { path: '', redirect: '/manager/profile' },
    { path: 'profile', component: MProfile },
    { path: 'users', component: MUsers },
    { path: 'projects', component: MProjects },
    { path: 'defects', component: MDefects },
  ]},

  { path: '/engineer', component: DashboardLayout, meta: { role: 'Engineer' }, children: [
    { path: '', redirect: '/engineer/profile' },
    { path: 'profile', component: EProfile },
    { path: 'projects', component: EProjects },
    { path: 'defects', component: EDefects },
  ]},

  { path: '/customer', component: DashboardLayout, meta: { role: 'Customer' }, children: [
    { path: '', redirect: '/customer/profile' },
    { path: 'profile', component: CProfile },
    { path: 'projects', component: CProjects },
    { path: 'reports', component: CReports },
  ]},

  { path: '/:pathMatch(.*)*', redirect: '/' }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.token && to.name !== 'auth') return { name: 'auth' }

  if (auth.token && !auth.user) {
    try {
      await auth.fetchProfile()
    } catch (e) {
      console.warn('[router] fetchProfile failed', e?.response?.status, e?.config?.url)
    }
  }

  const needRole = to.meta?.role
  if (needRole && auth.user?.role !== needRole) {
    if (!auth.user?.role) return { name: 'auth' }
    if (auth.user.role === 'Manager') return { path: '/manager' }
    if (auth.user.role === 'Engineer') return { path: '/engineer' }
    if (auth.user.role === 'Customer') return { path: '/customer' }
  }
})

export default router
