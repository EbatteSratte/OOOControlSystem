<template>
  <div class="grid cols-1" style="gap:16px;">
    <div class="card">
      <h2 class="section-title">Мои проекты</h2>
      <ProjectTable :allow-edit="false" :onlyMine="true"/>
    </div>
    <div class="card">
      <h2 class="section-title">Дефекты по проектам</h2>
      <DefectTable :customerOwnedOnly="true" hide-actions
        :manager="true"
        :allow-assign="false"
        :can-edit="false"
        :can-change-status="false"
        :customer-owned-only="true"
        :allow-upload="false"
      />
    </div>
  </div>
</template>
<script setup>
import ProjectTable from '../../components/ProjectTable.vue'
import DefectTable from '../../components/DefectTable.vue'
</script>
