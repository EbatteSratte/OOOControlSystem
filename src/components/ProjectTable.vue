<template>
  <div class="card">
    <div class="row" style="justify-content:space-between; align-items:center;">
      <h2 class="section-title">Проекты</h2>
      <div class="row tools" style="display:flex;align-items:center;gap:8px;flex-wrap:nowrap;">
        <button v-if="allowCreate" class="btn" type="button" @click="$emit('create')">Создать</button>
        <input v-model="searchQuery" @input="filter" placeholder="Поиск: название проекта" class="input" style="min-width:260px;flex:1 1 320px;" />
        <button class="btn secondary" @click="load">Обновить</button>
      </div>
    </div>
    <table class="table">
      <thead><tr><th>ID</th><th>Название</th><th>Статус</th><th>Владелец</th><th style="width:320px">Действия</th></tr></thead>
      <tbody>
        <tr v-for="p in view" :key="p.id">
          <td>{{ p.id }}</td><td>{{ p.name }}</td><td>{{ p.status }}</td><td>{{ p.owner?.fullName }}</td>
          <td class="row right">
            <button class="btn ghost" @click="info=p">Информация</button>
            <button v-if="props.allowEdit" class="btn" @click="edit={...p}">Редактировать</button>
        <button v-if="props.allowDelete" class="btn small danger" @click="removeProject(p)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>
    <Modal v-if="info" title="Информация о проекте" @close="info=null">
      <div class="grid cols-2">
        <div><label>ID</label><div>{{ info.id }}</div></div>
        <div><label>Название</label><div>{{ info.name }}</div></div>
        <div><label>Статус</label><div>{{ info.status }}</div></div>
        <div><label>Владелец</label><div>{{ info.owner?.fullName }}</div></div>
        <div><label>Описание</label><div>{{ info.description }}</div></div>
        <div><label>Создан</label><div>{{ fmt(info.createdAt) }}</div></div>
      </div>
    </Modal>
    <Modal v-if="edit" title="Редактировать проект" cardClass="form-shell" @close="edit=null">
      <div class="form-body" style="--field-w:280px">
        <div><label>Название</label><input class="input" v-model="edit.name" /></div>
        <div><label>Статус</label>
          <select class="select" v-model="edit.status"><option>Active</option><option>Completed</option><option>OnHold</option></select>
        </div>
        <div class="col-span-2"><label>Описание</label><textarea class="textarea" v-model="edit.description"/></div>
      </div>
      <div class="form-actions"><button class="btn" @click="saveProject">Сохранить</button></div>
    </Modal>
  </div>
</template>
<script setup>
const props = defineProps({allowEdit:   { type: Boolean, default: true },
  allowCreate: { type: Boolean, default: false },
  onlyMine:    { type: Boolean, default: false }, 
  allowDelete: { type: Boolean, default: false }
})

import { ref, computed, onMounted } from 'vue'
import api from '../api'
import Modal from './Modal.vue'
import { fmtDate as fmt } from '../utils/format'
const items = ref([])
const searchQuery = ref('')
const view = computed(()=> (items.value||[]).filter(p => (p.name||'').toLowerCase().includes(searchQuery.value.trim().toLowerCase())))
function filter(){}
const info = ref(null)
const edit = ref(null)
async function load(){
  try {
    if (props.onlyMine) {
      const { data: profile } = await api.get('/api/users/get-profile')
      // Support both camelCase and PascalCase just in case
      const owned = profile?.ownedProjects ?? profile?.OwnedProjects ?? []
      const result = []
      for (const p of owned) {
        // Some backends return nested {id,name} or just ids; normalize
        const pid = typeof p === 'number' ? p : (p.id ?? p.Id)
        if (!pid) continue
        const { data } = await api.get(`/api/projects/${pid}`)
        if (data) result.push({
          id: data.id, name: data.name, description: data.description,
          status: data.status, createdAt: data.createdAt, owner: data.owner
        })
      }
      items.value = result
      return
    }
    const { data } = await api.get('/api/projects')
    items.value = data || []
  } catch (e) {
    console.error('[ProjectTable] load failed', e)
    items.value = []
  }
}
async function saveProject(){
  const payload = { name: edit.value.name, description: edit.value.description, status: edit.value.status }
  await api.put(`/api/projects/${edit.value.id}`, payload)
  edit.value=null
  await load()
}
onMounted(load)


async function removeProject(row){
  if(!confirm('Удалить проект #' + row.id + ' ?')) return;
  await api.delete(`/api/projects/${row.id}`)
  await load()
}
</script>

<style>
.table td.row{ display:flex; gap:8px; align-items:center; justify-content:flex-end; flex-wrap:nowrap; }
</style>
