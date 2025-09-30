<template>
  <div style="min-height:100vh; display:grid; place-items:center; padding:24px;">
    <div class="card" style="max-width: 880px; width:100%; display:flex; flex-direction:column; gap:16px; text-align:center;">
      <h1 style="font-size:28px; margin-bottom:0;">OOO Control System</h1>
      <p style="color:#cbd5e1; font-size:16px; margin:0;">
        Централизованное управление дефектами: от регистрации и назначения исполнителя до контроля сроков и аналитики.
        Роли: инженеры (фиксация и обновления), менеджеры (назначения и контроль), руководители и заказчики (просмотр прогресса).
      </p>
      <div class="row" style="justify-content:center; margin-top:8px;">
        <button class="btn" @click="open('login')">Войти</button>
        <button class="btn ghost" @click="open('register')">Зарегистрироваться</button>
      </div>
    </div>

    <div class="auth-modal">
      <Modal v-if="mode" cardClass="form-shell" :title="mode==='login' ? 'Вход' : 'Регистрация'" @close="mode=''">
      <div v-if="mode==='register'" class="form-body" style="--field-w:200px">
        <div style="width:200px">
          <label>ФИО</label>
          <input class="input" v-model="fullName" placeholder="Иван Иванов" style="width:200px" />
        </div>
      </div>
      <div class="form-body" style="--field-w:200px">
        <div><label>Email</label><input class="input" type="email" v-model="email" /></div>
        <div><label>Пароль</label><input class="input" type="password" v-model="password" /></div>
        <div v-if="mode==='register'"><label>Повтор пароля</label><input class="input" type="password" v-model="password2" /></div>
      </div>
      <div class="form-actions">
        <button class="btn" :disabled="loading" @click="submit">{{ mode==='login' ? 'Войти' : 'Зарегистрироваться' }}</button>
      </div>
      <div v-if="error" style="color:#fda4af; margin-top:8px;">{{ error }}</div>
      </Modal>
    </div>
  </div>
</template>
<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Modal from '../components/Modal.vue'

const auth = useAuthStore(); const router = useRouter()
const mode = ref('') // '', 'login', 'register'
const fullName = ref(''); const email=ref(''); const password=ref(''); const password2=ref(''); const loading=ref(false); const error=ref('')

function open(m){
  mode.value = m;
  error.value='';
  email.value='';
  password.value='';
  if (m==='register') fullName.value='';
  password2.value='';
}
async function submit(){
  loading.value=true; error.value=''
  try{
    if(mode.value==='login') await auth.login({ email: email.value, password: password.value })
    else {
      if (!fullName.value) { error.value = 'Укажите ФИО'; throw new Error('no_fullName') }
      if (!email.value) { error.value = 'Укажите email'; throw new Error('no_email') }
      if (!password.value) { error.value = 'Укажите пароль'; throw new Error('no_password') }
      if (password.value !== password2.value) { error.value = 'Пароли не совпадают'; throw new Error('password_mismatch') }
      await auth.register({ fullName: fullName.value, email: email.value, password: password.value })
    }
    router.push(auth.user?.role==='Manager' ? '/manager' : '/engineer')
  }catch(e){
    error.value = e?.response?.data?.message || e?.response?.data?.Message || 'Пароли не совпадают'
  }finally{
    loading.value=false
  }
}
</script>

<style>.auth-modal .form-200{width:200px; max-width:200px;}.auth-modal .column .input{width:100%}</style><style>.column .input{width:100%}.auth-modal .column .input{width:100%}</style>