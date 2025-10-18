<script setup lang="ts">
import {
  SidebarProvider,
} from "@/components/ui/sidebar";
import ConversationSidebar from "./ConversationSidebar.vue";
</script>

<template>
  <SidebarProvider>
    <ConversationSidebar />
    <main class="flex flex-1 flex-col gap-4 p-4">
      <slot />
    </main>
  </SidebarProvider>
</template>
