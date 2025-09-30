<template>
  <div class="card">
    <div class="row" style="justify-content:space-between;align-items:center">
      <h2 class="section-title">Дефекты</h2>
      <div class="row">
        <label><input type="checkbox" v-model="assignedOnly" /> Только назначенные мне</label>
      </div>
    </div>
    <DefectTable :allow-delete="true" :manager="false" :allow-assign="false" :can-edit="true" :can-change-status="true" :assigned-only="assignedOnly"  :allow-upload="true"/>
  </div>
</template>
<script setup>
import { ref } from 'vue'
import DefectTable from '../../components/DefectTable.vue'
const assignedOnly = ref(true)
</script>
