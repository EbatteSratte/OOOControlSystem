<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="brand">
        <div style="width:10px;height:10px;background:var(--primary);border-radius:3px;"></div>
        <div class="brand__title">OOO Control System</div>
      </div>
      <nav class="nav" v-if="role==='Manager'">
        <RouterLink to="/manager/profile" :class="{active:isActive('/manager/profile')}">👤 Профиль</RouterLink>
        <RouterLink to="/manager/users" :class="{active:isActive('/manager/users')}">👥 Пользователи</RouterLink>
        <RouterLink to="/manager/projects" :class="{active:isActive('/manager/projects')}">📁 Проекты</RouterLink>
        <RouterLink to="/manager/defects" :class="{active:isActive('/manager/defects')}">🐞 Дефекты</RouterLink>
        <a href="#" @click.prevent="logout" class="btn ghost" style="margin-top:8px;">Выйти</a>
      </nav>
      <nav class="nav" v-else-if="role==='Engineer'">
        <RouterLink to="/engineer/profile" :class="{active:isActive('/engineer/profile')}">👤 Профиль</RouterLink>
        <RouterLink to="/engineer/projects" :class="{active:isActive('/engineer/projects')}">📁 Проекты</RouterLink>
        <RouterLink to="/engineer/defects" :class="{active:isActive('/engineer/defects')}">🐞 Дефекты</RouterLink>
        <a href="#" @click.prevent="logout" class="btn ghost" style="margin-top:8px;">Выйти</a>
      </nav>
      <nav class="nav" v-else-if="role==='Customer'">
        <RouterLink to="/customer/profile" :class="{active:isActive('/customer/profile')}">👤 Профиль</RouterLink>
        <RouterLink to="/customer/projects" :class="{active:isActive('/customer/projects')}">📁 Проекты</RouterLink>
        <RouterLink to="/customer/reports" :class="{active:isActive('/customer/reports')}">📊 Отчёты</RouterLink>
        <a href="#" @click.prevent="logout" class="btn ghost" style="margin-top:8px;">Выйти</a>
      </nav>
      <nav class="nav" v-else>
        <RouterLink to="/engineer/profile" :class="{active:isActive('/engineer/profile')}">👤 Профиль</RouterLink>
        <RouterLink to="/engineer/projects" :class="{active:isActive('/engineer/projects')}">📁 Проекты</RouterLink>
        <RouterLink to="/engineer/defects" :class="{active:isActive('/engineer/defects')}">🐞 Дефекты</RouterLink>
        <a href="#" @click.prevent="logout" class="btn ghost" style="margin-top:8px;">Выйти</a>
      </nav>
    </aside>
    <header class="topbar">
      <h1>{{ title }}</h1>
      <div class="right"><span class="badge">{{ user?.fullName }} ({{ user?.role }})</span></div>
    </header>
    <main class="main"><RouterView /></main>
  </div>
</template>
<script setup>
import { useRoute, useRouter } from 'vue-router'
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'
const route = useRoute(); const router = useRouter()
const auth = useAuthStore()
const user = computed(()=>auth.user)
const role = computed(()=>auth.user?.role ?? '')
const title = computed(() => {
  const p = route.path
  if (p.includes('/profile')) return 'Профиль'
  if (p.includes('/users')) return 'Пользователи'
  if (p.includes('/projects')) return 'Проекты'
  if (p.includes('/defects')) return 'Дефекты'
  if (p.includes('/reports')) return 'Отчёты'
  return 'Панель'
})
const logout = () => { auth.logout(); router.push('/') }
const isActive = (p) => route.path === p
</script>
