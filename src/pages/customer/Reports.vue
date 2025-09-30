<template>
  <div class="card">
    <div class="row reports-header">
      <h2 class="section-title">Отчёты</h2>
      <div class="row reports-toolbar">
        <select class="select" v-model.number="projectId" style="min-width:220px;flex:0 0 auto;">
          <option :value="null">Все проекты</option>
          <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
        <button class="btn" @click="reload" style="flex:0 0 auto;">Обновить</button>
        <button class="btn secondary" @click="downloadExcel" :disabled="downloading" style="flex:0 0 auto;">
          {{ downloading ? 'Скачиваю…' : 'Скачать отчёт' }}
        </button>
      </div>
    </div>

    <div class="grid cols-2" style="gap:12px; margin-top:12px;">
      <!-- Created vs Closed by week -->
      <div class="panel">
        <h3 class="panel__title">Дефекты: создано / закрыто (по неделям)</h3>
        <div v-if="flowRows.length === 0" class="muted">Нет данных</div>
        <div v-else ref="elFlow" style="height:320px"></div>
      </div>

      <!-- Age buckets -->
      <div class="panel">
        <h3 class="panel__title">Возраст бэклога</h3>
        <div v-if="ageRows.length === 0" class="muted">Нет данных</div>
        <div v-else ref="elAge" style="height:320px"></div>
      </div>

      <!-- Heatmap (7x24) -->
      <div class="panel">
        <h3 class="panel__title">Активность по времени (heatmap)</h3>
        <div v-if="heatMax === 0" class="muted">Нет данных</div>
        <div v-else ref="elHeat" style="height:360px"></div>
      </div>

      <!-- Status by project -->
      <div class="panel">
        <h3 class="panel__title">Статусы по проектам</h3>
        <div v-if="statusRows.length === 0" class="muted">Нет данных</div>
        <div v-else ref="elStatus" style="height:360px"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch, computed, nextTick } from 'vue'
import api from '../../api'

// ---------- state ----------
const projects   = ref([])     // только проекты текущего заказчика
const defects    = ref([])     // только дефекты по этим проектам
const projectId  = ref(null)   // фильтр "все" или конкретный проект
const downloading = ref(false)

// echarts containers
const elFlow   = ref(null)
const elAge    = ref(null)
const elHeat   = ref(null)
const elStatus = ref(null)

// chart instances
let charts = { flow:null, age:null, heat:null, status:null }

// ---------- utils ----------
function getCreatedAt(d){ return d.createdAt || d.CreatedAt || d.created_at || null }
function getUpdatedAt(d){ return d.updatedAt || d.UpdatedAt || null }
function normalizeStatus(s){
  s = (s||'').toString().toLowerCase()
  if (/new|created/.test(s)) return 'New'
  if (/progress|work/.test(s)) return 'InProgress'
  if (/hold|wait/.test(s)) return 'OnHold'
  if (/resolve|fixed|done|close/.test(s)) return 'Resolved'
  return 'Unknown'
}
function getProjectId(d){ return d.projectId ?? d.ProjectId ?? d.project?.id ?? d.Project?.Id ?? null }
function getProjectName(d, nameById){ return d.project?.name || d.Project?.Name || nameById.get(getProjectId(d)) || 'Без проекта' }
function weekKey(dt){
  const d = new Date(dt); if (isNaN(d)) return null
  const day = (d.getDay()+6)%7 // ISO Monday
  const monday = new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()))
  monday.setUTCDate(monday.getUTCDate()-day)
  const y = monday.getUTCFullYear()
  const m = String(monday.getUTCMonth()+1).padStart(2,'0')
  const da = String(monday.getUTCDate()).padStart(2,'0')
  return `${y}-${m}-${da}`
}
const hours = Array.from({length:24}, (_,i)=>String(i).padStart(2,'0')+':00')
const days  = ['Пн','Вт','Ср','Чт','Пт','Сб','Вс']

// разрешённые проекты (только свои)
const allowedProjectIds = computed(() => new Set((projects.value||[]).map(p => Number(p.id))))

// отфильтрованные дефекты
const filtered = computed(() => {
  const pidSel = projectId.value
  const allowed = allowedProjectIds.value
  return (defects.value || []).filter(d => {
    const pid = Number(getProjectId(d))
    if (!allowed.has(pid)) return false
    if (pidSel == null) return true
    return pid === Number(pidSel)
  })
})

// ---------- datasets ----------
const flowRows = computed(() => {
  const created = {}
  const closed = {}
  for (const d of filtered.value){
    const c = getCreatedAt(d)
    if (c){ const k = weekKey(c); if (k) created[k] = (created[k]||0)+1 }
    if (normalizeStatus(d.status || d.Status) === 'Resolved'){
      const u = getUpdatedAt(d)
      if (u){ const k2 = weekKey(u); if (k2) closed[k2] = (closed[k2]||0)+1 }
    }
  }
  const keys = Array.from(new Set([...Object.keys(created), ...Object.keys(closed)])).sort()
  return keys.map(k => ({ week: k, created: created[k]||0, closed: closed[k]||0 }))
})

const ageRows = computed(() => {
  const now = new Date()
  const buckets = { '0–7':0, '8–14':0, '15–30':0, '31+':0 }
  for (const d of filtered.value){
    if (normalizeStatus(d.status || d.Status) === 'Resolved') continue
    const c = getCreatedAt(d); if (!c) continue
    const age = Math.floor((now - new Date(c))/(1000*60*60*24))
    if (age<=7) buckets['0–7']++
    else if (age<=14) buckets['8–14']++
    else if (age<=30) buckets['15–30']++
    else buckets['31+']++
  }
  return Object.entries(buckets).map(([label, count]) => ({ label, count }))
})

const heatMatrix = computed(() => {
  const m = Array.from({length:7},()=>Array(24).fill(0))
  for (const d of filtered.value){
    const c = getCreatedAt(d); if (!c) continue
    const dt = new Date(c); if (isNaN(dt)) continue
    const dow = (dt.getDay()+6)%7
    const h = dt.getHours()
    m[dow][h]++
  }
  return m
})
const heatMax = computed(() => Math.max(0, ...heatMatrix.value.flat()))

const statusRows = computed(() => {
  const byProject = new Map()
  const nameById = new Map((projects.value||[]).map(p => [Number(p.id), p.name]))
  for (const d of filtered.value){
    const name = getProjectName(d, nameById)
    const st = normalizeStatus(d.status || d.Status)
    const entry = byProject.get(name) || { name, New:0, InProgress:0, OnHold:0, Resolved:0, total:0 }
    if (entry[st] != null) entry[st]++
    entry.total++
    byProject.set(name, entry)
  }
  return Array.from(byProject.values()).sort((a,b)=>b.total-a.total)
})

// ---------- echarts loader (CDN only, без import) ----------
async function ensureEcharts(){
  if (window.echarts) return window.echarts
  await new Promise((resolve, reject) => {
    const id = 'echarts-cdn'
    if (document.getElementById(id)) return resolve()
    const s = document.createElement('script')
    s.id = id
    s.src = 'https://cdn.jsdelivr.net/npm/echarts@5/dist/echarts.min.js'
    s.onload = resolve
    s.onerror = reject
    document.head.appendChild(s)
  })
  return window.echarts
}

// ---------- renderers ----------
async function renderAll(){
  const echarts = await ensureEcharts()
  await nextTick()
  renderFlow(echarts)
  renderAge(echarts)
  renderHeat(echarts)
  renderStatus(echarts)
}

function disposeChart(key){
  if (charts[key]) { charts[key].dispose(); charts[key] = null }
}

function renderFlow(echarts){
  disposeChart('flow')
  if (!elFlow.value || flowRows.value.length === 0) return
  const x = flowRows.value.map(r=>r.week)
  const created = flowRows.value.map(r=>r.created)
  const closed  = flowRows.value.map(r=>r.closed)
  charts.flow = echarts.init(elFlow.value)
  charts.flow.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data:['Создано','Закрыто'] },
    grid: { left: 40, right: 20, top: 30, bottom: 40 },
    xAxis: { type:'category', data: x, axisLabel:{ rotate: 45 } },
    yAxis: { type:'value' },
    series: [
      { name:'Создано', type:'bar', data: created },
      { name:'Закрыто', type:'bar', data: closed }
    ]
  })
}

function renderAge(echarts){
  disposeChart('age')
  if (!elAge.value || ageRows.value.length === 0) return
  const cats = ageRows.value.map(r=>r.label)
  const vals = ageRows.value.map(r=>r.count)
  charts.age = echarts.init(elAge.value)
  charts.age.setOption({
    tooltip: { trigger:'axis' },
    grid: { left: 40, right: 20, top: 30, bottom: 30 },
    xAxis: { type:'category', data: cats },
    yAxis: { type:'value' },
    series: [{ type:'bar', data: vals }]
  })
}

function renderHeat(echarts){
  disposeChart('heat')
  if (!elHeat.value || heatMax.value === 0) return
  const data = []
  for (let y=0; y<7; y++){
    for (let x=0; x<24; x++){
      data.push([x, y, heatMatrix.value[y][x]])
    }
  }
  charts.heat = echarts.init(elHeat.value)
  charts.heat.setOption({
    tooltip: {
      position: 'top',
      formatter: (p)=> `${days[p.value[1]]}, ${hours[p.value[0]]}: <b>${p.value[2]}</b>`
    },
    grid: { left: 60, right: 20, top: 20, bottom: 40 },
    xAxis: { type:'category', data: hours, splitArea:{ show:true } },
    yAxis: { type:'category', data: days, splitArea:{ show:true } },
    visualMap: {
      min: 0, max: heatMax.value, calculable: true, orient: 'horizontal',
      left: 'center', bottom: 0
    },
    series: [{
      name: 'Активность',
      type: 'heatmap',
      data,
      emphasis: { itemStyle: { shadowBlur: 10, shadowColor: 'rgba(0,0,0,0.5)' } }
    }]
  })
}

function renderStatus(echarts){
  disposeChart('status')
  if (!elStatus.value || statusRows.value.length === 0) return
  const names = statusRows.value.map(r=>r.name)
  const sNew  = statusRows.value.map(r=>r.New||0)
  const sProg = statusRows.value.map(r=>r.InProgress||0)
  const sHold = statusRows.value.map(r=>r.OnHold||0)
  const sRes  = statusRows.value.map(r=>r.Resolved||0)
  charts.status = echarts.init(elStatus.value)
  charts.status.setOption({
    tooltip: { trigger:'axis', axisPointer:{ type:'shadow' } },
    legend: { data:['New','InProgress','OnHold','Resolved'] },
    grid: { left: 120, right: 20, top: 30, bottom: 30 },
    xAxis: { type:'value' },
    yAxis: { type:'category', data: names },
    series: [
      { name:'New',        type:'bar', stack:'total', data:sNew },
      { name:'InProgress', type:'bar', stack:'total', data:sProg },
      { name:'OnHold',     type:'bar', stack:'total', data:sHold },
      { name:'Resolved',   type:'bar', stack:'total', data:sRes }
    ]
  })
}

// ---------- data loading ----------
async function reload(){
  // профиль
  let profile = null
  try {
    const { data } = await api.get('/api/users/get-profile')
    profile = data || null
  } catch (e) {
    console.warn('[Reports] cannot load profile', e)
  }

  // списки
  const [pRes, dRes] = await Promise.all([ api.get('/api/projects'), api.get('/api/defects') ])
  const allProjects = pRes.data || []
  defects.value = dRes.data || []

  // разрешённые id
  const allowed = new Set()
  if (profile) {
    const owned = profile.ownedProjects ?? profile.OwnedProjects ?? []
    if (Array.isArray(owned)) {
      for (const p of owned) {
        const id = typeof p === 'number' ? p : (p?.id ?? p?.Id)
        if (id != null) allowed.add(Number(id))
      }
    }
    const meId = profile.id ?? profile.Id
    for (const p of allProjects) {
      const pid = p.id ?? p.Id
      const ownerId = p.ownerId ?? p.OwnerId
      if (ownerId != null && meId != null && Number(ownerId) === Number(meId)) {
        allowed.add(Number(pid))
      }
    }
  }

  // нормализованные проекты только свои
  projects.value = allProjects
    .filter(p => allowed.has(Number(p.id ?? p.Id)))
    .map(p => ({ id: (p.id ?? p.Id), name: (p.name ?? p.Name) }))

  // если выбран недоступный проект — сбрасываем
  if (projectId.value != null && !projects.value.some(p => Number(p.id) === Number(projectId.value))) {
    projectId.value = null
  }

  await nextTick()
  renderAll()
}

async function downloadExcel(){
  try{
    downloading.value = true
    const res = await api.get('/api/reports/customer-excel', { responseType:'blob' })
    let filename = 'customer_report.xlsx'
    const cd = res.headers?.['content-disposition']
    if (cd){
      const m = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/i.exec(cd)
      if (m && m[1]) filename = decodeURIComponent(m[1].replace(/['"]/g,''))
    }
    const blob = new Blob([res.data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = filename
    document.body.appendChild(a); a.click(); a.remove()
    URL.revokeObjectURL(url)
  }catch(e){
    console.error(e); alert('Не удалось скачать отчёт')
  }finally{
    downloading.value = false
  }
}

// ---------- lifecycle & watchers ----------
let resizeHandler = null
onMounted(async () => {
  await reload()
  resizeHandler = () => {
    for (const k of Object.keys(charts)) if (charts[k]) charts[k].resize()
  }
  window.addEventListener('resize', resizeHandler)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resizeHandler)
  for (const k of Object.keys(charts)) disposeChart(k)
})

watch(projectId, async () => { await nextTick(); renderAll() })
watch(defects,     async () => { await nextTick(); renderAll() })
</script>

<style>
.reports-header{ display:flex; align-items:center; gap:12px; justify-content:space-between; flex-wrap:nowrap; }
.reports-toolbar{ display:flex; gap:8px; flex-wrap:nowrap; white-space:nowrap; align-items:center; }
.reports-toolbar .select{ width:auto; min-width:220px; flex:0 0 auto; }
.reports-toolbar .btn{ flex:0 0 auto; }

.panel { background: var(--panel,#0b1220); border:1px solid var(--border,#1f2937); border-radius:10px; padding:8px; }
.panel__title { margin:0 0 8px 0; font-size:16px; color:var(--muted,#cbd5e1); }

.muted{ color:var(--muted,#94a3b8); font-size:14px }
</style>
