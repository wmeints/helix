<script setup lang="ts">
import { onMounted } from "vue";
import { useCodingAgent } from "@/stores/coding-agent";
import MainLayout from "@/components/layout/MainLayout.vue";

const store = useCodingAgent();

onMounted(() => {
  store.connect();
});
</script>

<template>
  <MainLayout>
    <div class="flex flex-col items-center justify-center h-full text-center">
      <h1 class="text-4xl font-bold mb-4">Welcome to Helix</h1>
      <p class="text-muted-foreground">
        Start a new conversation or select an existing one from the sidebar.
      </p>
    </div>
  </MainLayout>
</template>
