<script setup lang="ts">
import { onMounted } from "vue";
import { useCodingAgent } from "@/stores/coding-agent";
import MainLayout from "@/components/layout/MainLayout.vue";
import ConversationView from "@/components/conversation/ConversationView.vue";

const store = useCodingAgent();

onMounted(() => {
  store.connect();
});
</script>

<template>
  <MainLayout>
    <ConversationView />
  </MainLayout>
</template>
