<script setup lang="ts">
import {
  SidebarProvider,
  SidebarInset
} from "@/components/ui/sidebar";
import ConversationSidebar from "./ConversationSidebar.vue";
</script>

<template>
  <SidebarProvider>
    <ConversationSidebar />
    <SidebarInset>
      <slot />
    </SidebarInset>
  </SidebarProvider>
</template>
