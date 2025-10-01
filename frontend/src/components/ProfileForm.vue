<template>
  <div class="card">
    <h2 class="section-title">Профиль</h2>
    <div class="grid cols-2">
      <div><label>ФИО</label><input class="input" v-model="model.fullName" /></div>
      <div><label>Email</label><input class="input" v-model="model.email" /></div>
      <div><label>Дата регистрации</label><div class="input" style="background:#0d1528;border:1px dashed var(--border)">{{ fmt(auth.user?.createdAt) || '—' }}</div></div>
    </div>
    <div class="row" style="margin-top:12px;"><button class="btn" @click="save">Сохранить</button></div>
  </div>
</template>
<script setup>
import { reactive } from 'vue'
import api from '../api'
import { useAuthStore } from '../stores/auth'
import { fmtDate as fmt } from '../utils/format'
const auth = useAuthStore()
const model = reactive({ fullName: auth.user?.fullName || '', email: auth.user?.email || '' })
async function save(){ await api.put(`/users/${auth.user.id}`, { fullName: model.fullName, email: model.email }); await auth.fetchProfile() }
</script>
