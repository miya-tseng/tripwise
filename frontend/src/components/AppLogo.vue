<script setup lang="ts">
withDefaults(defineProps<{ size?: number }>(), { size: 36 })
</script>

<template>
  <svg
    class="app-logo"
    :width="size"
    :height="size"
    viewBox="0 0 40 40"
    role="img"
    aria-label="TripWise"
  >
    <defs>
      <linearGradient id="logo-fill" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#2f7a6c" />
        <stop offset="1" stop-color="#16443f" />
      </linearGradient>
      <linearGradient id="logo-sun" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#f6c99a" />
        <stop offset="1" stop-color="#e48a5f" />
      </linearGradient>
    </defs>
    <rect width="40" height="40" rx="11" fill="url(#logo-fill)" />
    <circle cx="28.5" cy="11.5" r="4.2" fill="url(#logo-sun)" />
    <path
      d="M9 29c3.5-1 5-4 8-5.5s6 .5 9-2"
      fill="none"
      stroke="#fff"
      stroke-width="2"
      stroke-linecap="round"
      stroke-dasharray="0.1 4"
      opacity="0.85"
    />
    <path
      d="M28.5 18.5c-2.6 0-4.6 2-4.6 4.5 0 3.3 4.6 7.5 4.6 7.5s4.6-4.2 4.6-7.5c0-2.5-2-4.5-4.6-4.5Z"
      fill="#fff"
    />
    <circle cx="28.5" cy="23" r="1.7" fill="#1f5c56" />
    <circle cx="9" cy="29" r="2.2" fill="#fff" />
  </svg>
</template>

<style scoped>
.app-logo {
  flex-shrink: 0;
  display: block;
  filter: drop-shadow(0 6px 12px rgb(31 92 86 / 0.35));
}
</style>
