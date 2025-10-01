<template>
  <div>
    <select class="select" v-model.number="model" @change="$emit('update:modelValue', model)">
      <option :value="null">— Выберите проект —</option>
      <option v-for="p in list" :key="p.id" :value="p.id">#{{ p.id }} — {{ p.name }}</option>
    </select>
    <div v-if="loaded && !list.length" style="color:var(--muted); font-size:13px; margin-top:6px;">
      Нет доступных проектов. Обратитесь к менеджеру за доступом.
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted, computed } from 'vue'
import api from '../api'
import { useAuthStore } from '../stores/auth'
const props = defineProps({ modelValue: Number })
const model = ref(props.modelValue ?? null)
const projects = ref([])
const loaded = ref(false)
const auth = useAuthStore()
const list = computed(() => {
  if (projects.value?.length) return projects.value
  const own = auth.user?.OwnedProjects || auth.user?.ownedProjects || []
  const created = auth.user?.CreatedProjects || auth.user?.createdProjects || []
  const map = new Map()
  ;[...own, ...created].forEach(p => p && map.set(p.id, p))
  return Array.from(map.values())
})
onMounted(async () => {
  try {
    const { data } = await api.get('/projects')
    projects.value = data || []
  } catch (e) {
    projects.value = []
  } finally {
    loaded.value = true
  }
})
</script>
