<template>
  <div class="card">
    <div class="row" style="justify-content:space-between;align-items:center">
      <h2 class="section-title">Дефекты</h2>
      <div class="row tools" style="display:flex;align-items:center;gap:8px;flex-wrap:nowrap;">
  <input v-model="searchQuery" @input="onSearchInput" placeholder="Поиск: название/описание" class="input" style="min-width:260px;flex:1 1 320px;" />
  <select class="select" v-model.number="projectFilter" style="min-width:220px;">
    <option :value="null">Все проекты</option>
    <option v-for="p in projectOptions" :key="p.id" :value="p.id">#{{ p.id }} — {{ p.name }}</option>
  </select>
  <button v-if="!manager" class="btn" type="button" @click.prevent="showCreate=true">Создать</button>
  <button class="btn secondary" @click="load">Обновить</button>
</div>
    </div>

    <table class="table">
      <thead>
        <tr>
          <th>ID</th>
          <th>Заголовок</th>
          <th>Проект</th>
          <th>Статус</th>
          <th>Приоритет</th>
          <th>Исполнитель</th>
          <th style="width:320px">Действия</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="d in items" :key="d.id">
          <td>{{ d.id }}</td>
          <td>{{ d.title }}</td>
          <td>{{ d.project?.name || '—' }}</td>
          <td>{{ d.status }}</td>
          <td>{{ d.priority }}</td>
          <td>{{ d.assignedTo?.fullName || '—' }}</td>
          <td class="row right">            <button class="btn ghost" @click="openInfo(d.id)">Информация</button>
<template v-if="!hideActions">

            <button v-if="manager && allowAssign" class="btn secondary" @click="openAssign(d)">Назначить</button>
            <button v-if="canEdit" class="btn" @click="openEdit(d)">Редактировать</button>
            <button v-if="allowDelete" class="btn danger" @click="deleteDefect(d)">Удалить</button>
            <button v-if="canChangeStatus" class="btn secondary" @click="openStatus(d)">Изменить статус</button>
          
</template>
</td>
        </tr>
      </tbody>
    </table>
    <Modal v-if="showCreate" title="Новый дефект" @close="showCreate=false">
      <div class="form-body" style="--field-w:280px">
        <div style="width:100%">
          <label>Проект</label>
          <ProjectSelect v-model="createModel.projectId" />
        </div>
        <div style="width:100%">
          <label>Заголовок</label>
          <input class="input" v-model="createModel.title" placeholder="Краткое название" />
        </div>
        <div style="width:100%">
          <label>Описание</label>
          <textarea class="textarea" v-model="createModel.description" placeholder="Подробное описание"></textarea>
        </div>
        <div class="grid cols-2" style="width:100%">
          <div>
            <label>Приоритет</label>
            <select class="select" v-model="createModel.priority">
              <option>Low</option><option>Medium</option><option>High</option><option>Critical</option>
            </select>
          </div>
          <div>
            <label>Дедлайн</label>
            <input class="input" type="datetime-local" v-model="createModel.dueDateLocal" />
          </div>
        </div>
        <div class="row" style="justify-content:center;">
          <button class="btn" :disabled="creating" @click="createDefect">Создать</button>
        </div>
        <div v-if="createError" style="color:#fda4af;">{{ createError }}</div>
      </div>
    </Modal>


    <!-- Информация -->
    <Modal v-if="infoId" title="Информация о дефекте" @close="infoId=null">
      <div v-if="defect" class="grid cols-2">
        <div><b>ID:</b> {{ defect.id }}</div>
        <div><b>Проект:</b> {{ defect.project?.name || '—' }}</div>
        <div class="col-2"><b>Заголовок:</b> {{ defect.title }}</div>
        <div class="col-2"><b>Описание:</b> {{ defect.description || '—' }}</div>
        <div><b>Статус:</b> {{ defect.status }}</div>
        <div><b>Приоритет:</b> {{ defect.priority }}</div>
        <div><b>Исполнитель:</b> {{ defect.assignedTo?.fullName || '—' }}</div>
        <div><b>Репортёр:</b> {{ defect.reporter?.fullName || '—' }}</div>
        <div><b>Дедлайн:</b> {{ fmt(defect.dueDate) || '—' }}</div>
        <div><b>Создано:</b> {{ fmt(defect.createdAt) }}</div>
        <div><b>Обновлено:</b> {{ fmt(defect.updatedAt) }}</div>

        <div class="col-2">
          <b>История:</b>
          <ul>
            <li v-for="(h,idx) in (defect.history || [])" :key="idx">
              {{ fmt(h.changedAt) }} — {{ h.status }} — {{ h.description }} (by #{{ h.changedById }})
            </li>
          </ul>
        </div>

        <div class="col-2">
          <b>Фото:</b>
          <div class="gallery" v-if="(defect.attachmentPaths||[]).length">
            <a v-for="(p,idx) in defect.attachmentPaths" :key="idx" :href="asAttachmentUrl(p)" target="_blank" rel="noopener noreferrer">
              <img class="thumb" :src="asAttachmentUrl(p)" />
            </a>
          </div>
          <div v-else>Нет фото</div>
        </div>

        <div class="col-2" v-if="(manager || canEdit) && allowUpload">
          <label>Добавить фото</label>
          <input class="input" type="file" multiple @change="onFilesPicked" />
          <div class="row right" style="margin-top:8px;">
            <button class="btn" :disabled="uploading || !filesToUpload.length" @click="uploadFiles">{{ uploading ? 'Загрузка...' : 'Загрузить' }}</button>
          </div>
        </div>
      </div>
      <div v-else>Загрузка…</div>
    </Modal>

    <!-- Редактирование -->
    <Modal v-if="edit" title="Редактировать дефект" cardClass="form-shell" @close="edit=null"><div class="form-body" style="--field-w:280px">
        <div class="col-2"><label>Заголовок</label><input class="input" v-model="edit.title" /></div>
        <div class="col-2"><label>Описание</label><textarea class="input" v-model="edit.description" /></div>
        <div><label>Приоритет</label>
          <select class="select" v-model="edit.priority">
            <option>Low</option><option>Medium</option><option>High</option><option>Critical</option>
          </select>
        </div>
        <div><label>Дедлайн</label><input class="input" type="datetime-local" v-model="edit.dueDateLocal" /></div>
        <div class="form-actions"><button class="btn" @click="saveEdit">Сохранить</button></div>
      </div>
    </Modal>

    <!-- Изменение статуса -->
    <Modal v-if="statusEdit" title="Изменить статус" cardClass="form-shell" @close="statusEdit=null"><div class="form-body" style="--field-w:280px">
        <div><label>Статус</label>
          <select class="select" v-model="statusEdit.status">
            <option>New</option>
            <option>InProgress</option>
            <option>OnReview</option>
            <option>Closed</option>
            <option>Rejected</option>
          </select>
        </div>
        <div class="col-2"><label>Описание</label><textarea class="input" v-model="statusEdit.description" /></div>
        <div class="col-2 row right"><button class="btn" @click="saveStatus">Сохранить</button></div>
      </div>
    </Modal>

    <!-- Назначение (менеджер) -->
    <AssignEngineer v-if="assignModal" :defect-id="assignModal.id" :current-assignee-id="assignModal.assigneeId" @saved="onAssigned" @close="assignModal=null" />
  </div>
</template>

<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import api from '../api'
import Modal from './Modal.vue'
import AssignEngineer from './AssignEngineer.vue'
import ProjectSelect from './ProjectSelect.vue'
import { fmtDate as fmt } from '../utils/format'
const API_BASE = import.meta.env.VITE_API_BASE || ''
const showCreate = ref(false)
const creating = ref(false)
const createError = ref('')
const projectOptions = computed(() => projects.value)
const createModel = ref({ title: '', description: '', projectId: null, priority: 'Medium', dueDateLocal: '' })
const projects = ref([])
const projectFilter = ref(null)

const props = defineProps({
  hideActions: { type: Boolean, default: false },
  manager: { type: Boolean, default: false },
  allowAssign: { type: Boolean, default: true },
  canEdit: { type: Boolean, default: true },
  canChangeStatus: { type: Boolean, default: true },
  allowDelete: { type: Boolean, default: false },
  customerOwnedOnly: { type: Boolean, default: false },
  assignedOnly: { type: Boolean, default: false },
  allowUpload: { type: Boolean, default: false },
})
const searchQuery = ref('')
let searchDebounce = null
function onSearchInput(){ clearTimeout(searchDebounce); searchDebounce = setTimeout(() => load(), 300) }


const items = ref([])
const infoId = ref(null)
const defect = ref(null)
const edit = ref(null)
const statusEdit = ref(null)
const assignModal = ref(null)
const filesToUpload = ref([])
const uploading = ref(false)

function toLocalIso(iso) {
  if (!iso) return ''
  const dt = new Date(iso)
  const tz = new Date(dt.getTime() - dt.getTimezoneOffset()*60000)
  return tz.toISOString().slice(0,16)
}


async function createDefect(){
  creating.value = true; createError.value=''
  try{
    const dueIso = createModel.value.dueDateLocal ? new Date(createModel.value.dueDateLocal).toISOString() : null
    const payload = {
      Title: createModel.value.title, title: createModel.value.title,
      Description: createModel.value.description, description: createModel.value.description,
      ProjectId: createModel.value.projectId, projectId: createModel.value.projectId,
      Priority: createModel.value.priority, priority: createModel.value.priority,
      DueDate: dueIso, dueDate: dueIso
    }
    await api.post('/api/defects', payload)
    showCreate.value = false
    createModel.value = { title: '', description: '', projectId: null, priority: 'Medium', dueDateLocal: '' }
    await load()
  }catch(e){
    console.error('Create defect failed', e)
    createError.value = e?.response?.data?.message || e?.response?.data?.Message || 'Не удалось создать дефект'
  }finally{
    creating.value = false
  }
}
async function load() {
  // Customer owned defects aggregation
  if (props.customerOwnedOnly) {
    try {
      const { data: profile } = await api.get('/api/users/get-profile')
      const owned = profile?.ownedProjects ?? profile?.OwnedProjects ?? []
      const agg = []
      for (const p of owned) {
        const pid = typeof p === 'number' ? p : (p.id ?? p.Id)
        if (!pid) continue
        const { data } = await api.get(`/api/projects/${pid}`)
        for (const d of (data?.defects || [])) {
        const mapped = {
          id: d.id ?? d.Id,
          title: d.title ?? d.Title,
          description: d.description ?? d.Description,
          status: d.status ?? d.Status,
          priority: d.priority ?? d.Priority,
          project: { id: (data.id ?? data.Id), name: (data.name ?? data.Name) },
          reporter: d.reporter ?? d.Reporter,
          assignedTo: d.assignedTo ?? d.AssignedTo,
          dueDate: d.dueDate ?? d.DueDate,
          createdAt: d.createdAt ?? d.CreatedAt,
          updatedAt: d.updatedAt ?? d.UpdatedAt
        }
        agg.push(mapped)
      }
      }
      items.value = agg
      return
    } catch (e) { console.error('Failed to load customer-owned defects', e) }
  }
  // General list (Manager/Engineer). Support assignedOnly filter
  const params = {}
  if (projectFilter.value != null) params.projectId = projectFilter.value
  if (props.assignedOnly) {
    try {
      const { data: profile } = await api.get('/api/users/get-profile')
      params.assignedToId = profile?.id ?? profile?.Id
    } catch (e) { console.warn('Cannot resolve profile for assignedOnly', e) }
  }
  if (searchQuery.value && searchQuery.value.trim()) params.search = searchQuery.value.trim();
  const { data } = await api.get('/api/defects', { params })
  items.value = data || []
}

function openInfo(id){ infoId.value = id; reloadDefect() }
async function reloadDefect(){
  if (!infoId.value) return;
  defect.value = null;
  try {
    const { data } = await api.get(`/api/defects/${infoId.value}`)
    defect.value = data
  } catch (e) {
    console.error('[DefectTable] load details failed', e)
    defect.value = { id: infoId.value, title: 'Ошибка загрузки', description: 'Нет доступа или сервер вернул ошибку', attachmentPaths: [], history: [] }
  }
}

function openEdit(row){
  edit.value = { id: row.id, title: row.title, description: row.description || '', priority: row.priority, dueDateLocal: toLocalIso(row.dueDate) }
}
async function saveEdit(){
  const d = edit.value
  await api.put(`/api/defects/${d.id}`, { title: d.title, description: d.description, priority: d.priority, dueDate: d.dueDateLocal ? new Date(d.dueDateLocal).toISOString() : null })
  edit.value = null
  await load()
  if (infoId.value) await reloadDefect()
}

function openStatus(row){ statusEdit.value = { id: row.id, status: row.status, description: '' } }
async function saveStatus(){ const s = statusEdit.value; await api.put(`/api/defects/${s.id}/status`, { status: s.status, statusDescription: s.description }); statusEdit.value=null; await load(); if (infoId.value) await reloadDefect() }

function openAssign(row){ assignModal.value = { id: row.id, assigneeId: row.assignedTo?.id ?? null } }
async function onAssigned(){ assignModal.value=null; await load(); if (infoId.value) await reloadDefect() }
async function deleteDefect(row){
  if (!confirm(`Удалить дефект #${row.id}? Это действие необратимо.`)) return;
  try { await api.delete(`/api/defects/${row.id}`); if (infoId.value === row.id) infoId.value = null; await load() }
  catch(e){ alert("Не удалось удалить дефект: " + (e?.response?.data?.title || e.message)) }
}


function asAttachmentUrl(p){
  if (!defect.value) return '#'
  if (!p) return '#'
  // normalize backslashes -> forward slashes
  let s = String(p).replace(/\\/g, '/')
  // absolute URL
  if (/^https?:\/\//i.test(s)) return s
  // static uploads path from backend
  // fallback to API download route
  const fname = s.split('/').pop()
  return `${API_BASE}/api/defects/${defect.value.id}/attachments/${encodeURIComponent(fname)}`
}
function onFilesPicked(ev){
  filesToUpload.value = Array.from(ev.target.files || [])
}
async function uploadFiles(){
  if (!infoId.value || filesToUpload.value.length===0) return
  const form = new FormData()
  for (const f of filesToUpload.value) form.append('files', f)
  uploading.value = true
  try {
    await api.post(`/api/defects/${infoId.value}/attachments`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    filesToUpload.value = []
    await reloadDefect()
  } finally {
    uploading.value = false
  }
}

onMounted(load)
onMounted(loadProjects)
watch(() => projectFilter.value, () => { load() })
watch(() => props.assignedOnly, () => { load() })

async function loadProjects(){
  try{
    const { data } = await api.get('/api/projects')
    let list = (data || []).map(p => ({ id: p.id ?? p.Id, name: p.name ?? p.Name }))
    if (props.customerOwnedOnly){
      try{
        const { data: prof } = await api.get('/api/users/get-profile')
        const owned = prof?.ownedProjects ?? prof?.OwnedProjects ?? []
        const allowed = new Set(owned.map(x => (typeof x==='number'? Number(x) : Number(x?.id ?? x?.Id))))
        list = list.filter(p => allowed.has(Number(p.id)))
      }catch(e){ /* ignore */ }
    }
    projects.value = list
  }catch(e){ console.error('Не удалось загрузить проекты', e) }
}

function openCreate(){
  showCreate.value = true
  if (!projects.value.length) loadProjects()
}
</script>


<style>
.tools { display:flex; align-items:center; gap:8px; flex-wrap:nowrap; white-space:nowrap; }
.btn.danger { background: var(--danger); color: #fff; }
.input { padding:8px 10px; border:1px solid var(--border); border-radius: var(--radius-sm); }

.table td.row{ display:flex; gap:8px; align-items:center; justify-content:flex-end; flex-wrap:nowrap; }

.create-modal .modal__card{width:420px;max-width:none;padding:0}
.table td.row{ display:flex; gap:8px; align-items:center; justify-content:flex-end; flex-wrap:nowrap; }
</style>
