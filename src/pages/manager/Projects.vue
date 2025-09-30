<template>
  <ProjectTable :allow-delete="true" :allow-edit="true" :allow-create="true" @create="openCreate=true" :key="refreshKey"/>
  <Modal v-if="openCreate" title="Создать проект" cardClass="form-shell" @close="openCreate=false">
    <div class="form-body" style="--field-w:280px">
      <div><label>Название</label><input class="input" v-model="model.name"/></div>
      <div><label>Описание</label><textarea class="textarea" v-model="model.description"></textarea></div>
      <div><label>Владелец</label><UserSelect v-model="model.ownerId" role="Customer"/></div>
      <div class="form-actions"><button class="btn" @click="create">Создать</button></div>
    </div>
  </Modal>
</template>
<script setup>
import { ref } from 'vue'
import ProjectTable from '../../components/ProjectTable.vue'
import Modal from '../../components/Modal.vue'
import UserSelect from '../../components/UserSelect.vue'
import api from '../../api'
const openCreate = ref(false)
const refreshKey = ref(0)
const model = ref({ name:'', description:'', ownerId:null })
async function create(){
  const payload = { name: model.value.name, description: model.value.description, ownerId: model.value.ownerId }
  await api.post('/api/projects', payload)
  openCreate.value=false
  model.value = { name:'', description:'', ownerId:null }
  refreshKey.value++
}
</script>
