<script setup lang="ts">
import { RouterLink } from 'vue-router'
import AppLogo from '@/components/AppLogo.vue'
</script>

<template>
  <header class="app-bar" data-testid="app-bar">
    <div class="bar">
      <RouterLink to="/" class="brand" aria-label="TripWise home">
        <AppLogo :size="34" />
        <span class="wordmark">
          <span class="wordmark-name">TripWise</span>
          <span class="wordmark-tag">Plan your trips day by day</span>
        </span>
      </RouterLink>
      <div class="actions">
        <slot />
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-bar {
  position: sticky;
  top: 0;
  z-index: 40;
  background: rgb(245 242 236 / 0.78);
  border-bottom: 1px solid rgb(213 205 189 / 0.6);
  backdrop-filter: saturate(1.5) blur(14px);
}

.bar {
  max-width: 1040px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.7rem clamp(1rem, 3.5vw, 2.75rem);
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.7rem;
  color: inherit;
  text-decoration: none;
}

.wordmark {
  display: flex;
  flex-direction: column;
  line-height: 1.1;
}

.wordmark-name {
  font-family: var(--font-display);
  font-size: 1.35rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.wordmark-tag {
  font-size: 0.72rem;
  color: var(--color-muted);
  letter-spacing: 0.02em;
}

.actions {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

@media (max-width: 560px) {
  .wordmark-tag {
    display: none;
  }
}
</style>
