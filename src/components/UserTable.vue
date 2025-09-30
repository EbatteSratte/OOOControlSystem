<template>
  <div class="card">
    <div class="row" style="justify-content:space-between; align-items:center;">
  <h2 class="section-title">Пользователи</h2>
  <div class="row tools" style="display:flex;align-items:center;gap:8px;flex-wrap:nowrap;">
    <select class="select" v-model="scope" @change="load">
      <option value="active">Активные</option>
      <option value="all">Все</option>
    </select>
    <button class="btn secondary" @click="load">Обновить</button>
  </div>
</div>

    <table class="table">
      <thead>
        <tr><th>ID</th><th>ФИО</th><th>Email</th><th>Роль</th><th>Статус</th><th style="width:320px">Действия</th></tr>
      </thead>
      <tbody>
        <tr v-for="u in items" :key="u.id">
          <td>{{ u.id }}</td>
          <td>{{ u.fullName }}</td>
          <td>{{ u.email }}</td>
          <td>{{ u.role }}</td>
          <td><span class="badge">{{ u.isActive === false ? 'Не активен' : 'Активен' }}</span></td>
          <td class="row right">
            <button class="btn ghost" @click="openInfo(u)">Информация</button>
            <button class="btn" @click="edit = { ...u }">Редактировать</button>
          </td>
        </tr>
      </tbody>
    </table>

    <Modal v-if="info" title="Информация о пользователе" @close="info = null">
      <div class="grid cols-2">
        <div><label>ID</label><div>{{ info.id }}</div></div>
        <div><label>ФИО</label><div>{{ info.fullName }}</div></div>
        <div><label>Email</label><div>{{ info.email }}</div></div>
        <div><label>Роль</label><div>{{ info.role }}</div></div>
        <div><label>Создан</label><div>{{ fmt(info.createdAt) }}</div></div>
        <div><label>Статус</label><div>{{ info.isActive === false ? 'Не активен' : 'Активен' }}</div></div>
      </div>
      <div class="card" style="margin-top:10px;">
        <h2>Проекты, созданные пользователем</h2>
        <ul><li v-for="p in (info.createdProjects || info.CreatedProjects || [])" :key="p.id">#{{ p.id }} — {{ p.name }} ({{ p.status }})</li></ul>
      </div>
      <div class="card" style="margin-top:10px;">
        <h2>Проекты, владельцем которых является пользователь</h2>
        <ul><li v-for="p in (info.ownedProjects || info.OwnedProjects || [])" :key="p.id">#{{ p.id }} — {{ p.name }} ({{ p.status }})</li></ul>
      </div>
      <div class="card" style="margin-top:10px;">
        <h2>Дефекты, созданные (репортёр)</h2>
        <ul><li v-for="d in (info.reportedDefects || info.ReportedDefects || [])" :key="d.id">#{{ d.id }} — {{ d.title }} ({{ d.status }})</li></ul>
      </div>
      <div class="card" style="margin-top:10px;">
        <h2>Дефекты, назначенные пользователю</h2>
        <ul><li v-for="d in (info.assignedDefects || info.AssignedDefects || [])" :key="d.id">#{{ d.id }} — {{ d.title }} ({{ d.status }})</li></ul>
      </div>
    </Modal>

    <Modal v-if="edit" title="Редактировать пользователя" cardClass="form-shell" @close="edit = null">
      <div class="form-body" style="--field-w:280px">
        <div><label>ФИО</label><input class="input" v-model="edit.fullName" /></div>
        <div><label>Email</label><input class="input" v-model="edit.email" /></div>
        <div>
          <label>Роль</label>
          <select class="select" v-model="edit.role">
  <option value="Manager">Manager</option>
  <option value="Engineer">Engineer</option>
  <option value="Сustomer">Customer</option>
</select>
        </div>
        <div>
          <label>Активен</label>
          <select class="select" v-model="edit.isActive">
            <option :value="true">Да</option>
            <option :value="false">Нет</option>
          </select>
        </div>
      </div>
      <div class="form-actions">
        <button class="btn" @click="saveUser">Сохранить</button>
      </div>
    </Modal>
  </div>
</template>
<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '../api'
import Modal from './Modal.vue'
import { fmtDate as fmt } from '../utils/format'

const items = ref([])
const info = ref(null)
const edit = ref(null)
const scope = ref('active') // 'active' | 'all'

async function load () {
  if (scope.value === 'active') {
    const { data } = await api.get('/api/users', { params: { isActive: true } })
    items.value = data || []
  } else {
    const [a, b] = await Promise.all([
      api.get('/api/users', { params: { isActive: true } }).catch(()=>({ data: [] })),
      api.get('/api/users', { params: { isActive: false } }).catch(()=>({ data: [] })),
    ])
    const map = new Map()
    ;[...(a.data || []), ...(b.data || [])].forEach(u => map.set(u.id, u))
    items.value = Array.from(map.values())
  }
}

async function openInfo (u) {
  const params = { id: u.id }
  if (scope.value === 'active') params.isActive = true
  const { data } = await api.get('/api/users', { params })
  info.value = data
}

async function saveUser () {
  const payload = {
    fullName: edit.value.fullName,
    email: edit.value.email,
    role: edit.value.role,
    isActive: edit.value.isActive,
  }
  await api.put(`/api/users/${edit.value.id}`, payload)
  edit.value = null
  await load()
}

onMounted(load)
watch(scope, load)
</script>

<style>
.table td.row{ display:flex; gap:8px; align-items:center; justify-content:flex-end; flex-wrap:nowrap; }
</style>
