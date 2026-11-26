<script setup lang="ts">
import { RouterLink } from 'vue-router'

defineProps<{
  active: 'home' | 'all-trips'
}>()
</script>

<template>
  <nav class="app-nav" aria-label="Main navigation" data-testid="app-nav">
    <RouterLink
      :to="{ name: 'home' }"
      class="nav-item"
      :class="{ active: active === 'home' }"
      :aria-current="active === 'home' ? 'page' : undefined"
      data-testid="nav-home"
    >
      <span class="nav-icon" aria-hidden="true">🏠</span>
      <span class="nav-label">Home</span>
    </RouterLink>
    <RouterLink
      :to="{ name: 'home', query: { new: '1' } }"
      class="nav-item create"
      data-testid="nav-new-trip"
    >
      <span class="create-icon" aria-hidden="true">+</span>
      <span class="nav-label">New trip</span>
    </RouterLink>
    <RouterLink
      :to="{ name: 'all-trips' }"
      class="nav-item"
      :class="{ active: active === 'all-trips' }"
      :aria-current="active === 'all-trips' ? 'page' : undefined"
      data-testid="nav-all-trips"
    >
      <span class="nav-icon" aria-hidden="true">🧳</span>
      <span class="nav-label">All trips</span>
    </RouterLink>
  </nav>
</template>

<style scoped>
.app-nav {
  display: none;
}

@media (max-width: 720px) {
  .app-nav {
    position: fixed;
    inset: auto 0 0;
    z-index: 60;
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    align-items: end;
    padding: 0.3rem 0.4rem calc(0.3rem + env(safe-area-inset-bottom));
    background: rgb(255 255 255 / 0.94);
    border-top: 1px solid var(--color-border);
    box-shadow: 0 -6px 20px -12px rgb(27 36 49 / 0.25);
    backdrop-filter: saturate(1.4) blur(12px);
  }

  .nav-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.1rem;
    min-height: 3rem;
    padding: 0.3rem 0;
    font-size: 0.7rem;
    font-weight: 600;
    color: var(--color-muted);
    text-decoration: none;
    border-radius: 12px;
  }

  .nav-icon {
    font-size: 1.2rem;
    line-height: 1.3;
    filter: grayscale(1);
    opacity: 0.7;
  }

  .nav-item.active {
    color: var(--color-primary);
    background: var(--color-primary-soft);
  }

  .nav-item.active .nav-icon {
    filter: none;
    opacity: 1;
  }

  .create-icon {
    display: grid;
    place-items: center;
    width: 2.6rem;
    height: 2.6rem;
    margin-top: -1.1rem;
    font-size: 1.6rem;
    line-height: 1;
    color: #fff;
    background: linear-gradient(140deg, #2a6b5f 0%, #1a4f4a 100%);
    border: 3px solid #fff;
    border-radius: 50%;
    box-shadow: 0 8px 18px -8px rgb(31 92 86 / 0.7);
  }

  .create {
    color: var(--color-primary);
  }
}
</style>
