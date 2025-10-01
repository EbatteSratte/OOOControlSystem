<template>
  <select class="select" v-model.number="model" @change="$emit('update:modelValue', model)">
    <option :value="null">— Не назначено —</option>
    <option v-for="u in filtered" :key="u.id" :value="u.id">{{ u.fullName }}</option>
  </select>
</template>
<script setup>
import { ref, onMounted, computed } from 'vue'
import api from '../api'
const props = defineProps({ modelValue: Number, role: String, onlyActive: { type: Boolean, default: true } })
const model = ref(props.modelValue ?? null)
const users = ref([])
const filtered = computed(() => {
  return users.value.filter(u => (!props.role || (u.role === props.role)) )
})
onMounted(async () => {
  // always query active by default; backend default is isActive=true, but we pass explicitly to be clear
  const params = props.onlyActive ? { isActive: true } : {}
  const { data } = await api.get('/users', { params })
  users.value = data || []
})
</script>
