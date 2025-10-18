<script setup lang="ts">
import { onMounted } from "vue";
import { useCodingAgent } from "@/stores/coding-agent";
import {
    Sidebar,
    SidebarContent,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar";
import { Button } from "@/components/ui/button";
import { MessageSquarePlus } from "lucide-vue-next";

const store = useCodingAgent();

onMounted(async () => {
    await store.loadHistory();
});

function handleNewConversation() {
    store.reset();
}

function handleSelectConversation(id: string) {
    store.loadConversation(id);
}
</script>

<template>
    <Sidebar variant="inset" collapsible="none">
        <SidebarHeader>
            <div class="px-3">
                <h1 class="text-xl font-bold">Helix</h1>
            </div>
        </SidebarHeader>
        <SidebarContent>
            <div class="p-4">
                <Button @click="handleNewConversation" class="px-4" variant="outline">
                    <MessageSquarePlus class="mr-2 h-4 w-4" />
                    New Session
                </Button>
            </div>
            <SidebarGroup>
                <SidebarGroupLabel>Conversations</SidebarGroupLabel>
                <SidebarGroupContent>
                    <SidebarMenu>
                        <SidebarMenuItem>
                        </SidebarMenuItem>
                        <SidebarMenuItem v-for="conversation in store.history" :key="conversation.id">
                            <SidebarMenuButton @click="handleSelectConversation(conversation.id)">
                                {{ conversation.description }}
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                        <SidebarMenuItem v-if="store.history.length === 0">
                            <div class="text-sm text-muted-foreground px-2 py-1">
                                No conversations yet
                            </div>
                        </SidebarMenuItem>
                    </SidebarMenu>
                </SidebarGroupContent>
            </SidebarGroup>
        </SidebarContent>
    </Sidebar>
</template>
