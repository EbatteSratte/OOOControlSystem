<template>
  <div>
    <div v-if="items && items.length" class="gallery">
      <a v-for="(u,i) in items" :key="i" :href="abs(u)" target="_blank">
        <img :src="abs(u)" class="thumb" :alt="'attachment-'+i" @error="onErr($event)"/>
      </a>
    </div>
    <div v-else class="muted">Нет вложений</div>

    <div v-if="allowUpload" class="row" style="margin-top:8px;">
      <input type="file" multiple accept="image/*" @change="onFiles" />
      <button class="btn" @click="upload" :disabled="!files.length">Загрузить</button>
    </div>
  </div>
</template>
<script setup>
import { buildImageUrl } from '@/utils/imageUrl'
import { ref } from 'vue'
import api from '../api'
const props = defineProps({ defectId: Number, items: Array, allowUpload: Boolean })
const emit = defineEmits(['uploaded'])
const files = ref([])
function abs(u){
  if (!u) return u
  if (/^https?:\/\//i.test(u)) return u
  const base = ((import.meta.env.VITE_API_BASE ?? import.meta.env.VITE_API_URL ?? '') || '').replace(/\/$/, '')
  let rel = String(u).replace(/^\.\//, '')
  if (/^[\w.-]+\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(rel)) {
    rel = `/uploads/defects/${props.defectId}/${rel}`
  } else if (!rel.startsWith('/')) {
    rel = '/' + rel
  }
  return `${base}${rel}`
}
function onErr(e){ e.target.style.opacity = .4; e.target.title = 'Не удалось загрузить изображение' }
function onFiles(e){ files.value = Array.from(e.target.files || []) }
async function upload(){
  if (!files.value.length) return
  const fd = new FormData()
  files.value.forEach(f => fd.append('files', f))
  await api.post(`/defects/${props.defectId}/attachments`, fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  files.value = []
  emit('uploaded')
}
</script>
<style scoped>
.muted { color: var(--muted); font-size: 14px; }
</style>
