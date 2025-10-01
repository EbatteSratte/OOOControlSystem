<template>
  <Modal title="Назначить исполнителя" cardClass="form-shell" @close="$emit('close')"><div class="form-body" style="--field-w:280px">
      <label>Инженер</label>
      <select class="select" v-model="selectedId">
        <option :value="null">— Не назначен —</option>
        <option v-for="u in engineers" :key="u.id" :value="u.id">{{ u.fullName }} ({{ u.email }})</option>
      </select>
      <div class="form-actions">
        <button class="btn ghost" @click="$emit('close')">Отмена</button>
        <button class="btn" @click="save" :disabled="saving">{{ saving ? 'Сохранение...' : 'Сохранить' }}</button>
      </div>
    </div>
  </Modal>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'
import Modal from './Modal.vue'
const props = defineProps({ defectId: { type: Number, required: true }, currentAssigneeId: { type: Number, default: null } })
const emit = defineEmits(['close','saved'])
const engineers = ref([])
const selectedId = ref(props.currentAssigneeId ?? null)
const saving = ref(false)
async function loadEngineers(){
  const { data } = await api.get('/users', { params: { isActive: true } })
  engineers.value = (data || []).filter(u => u.role === 'Engineer')
}
async function save(){
  try {
    saving.value = true
    await api.put(`/defects/${props.defectId}/assign`, { assignedToId: selectedId.value })
    emit('saved', selectedId.value)
    emit('close')
  } finally {
    saving.value = false
  }
}
onMounted(loadEngineers)
</script>
