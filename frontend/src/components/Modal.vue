<template>
  <div class="modal" @click.self="$emit('close')">
    <div class="modal__card" :class="cardClass">
  <div class="modal__head">
    <h2 class="modal__title">{{ title }}</h2>
    <button class="btn ghost modal__close" @click="$emit('close')">Закрыть</button>
  </div>
  <div class="modal__body">
    <slot />
  </div>
</div>
</div>
</template>
<script setup>
defineProps({ title: String, cardClass: [String, Array, Object] })
</script>

<style>
.modal__head{position:relative;display:flex;align-items:center;justify-content:center;margin-bottom:10px}
.modal__title{margin:0;text-align:center}
.modal__close{position:absolute;right:0}

.modal__card{ background: var(--panel,#0b1220); border:1px solid var(--border,#1f2937); border-radius:10px; padding:0; max-width: 90vw; }
.modal__head{ position:relative; display:flex; align-items:center; justify-content:center; padding:10px 14px; border-bottom:1px solid var(--border,#1f2937); }
.modal__title{ margin:0; text-align:center; font-size:18px; line-height:24px; }
.modal__close{ position:absolute; right:10px; top:50%; transform: translateY(-50%); }
.modal__body{ padding:12px; }
</style>
