<script setup lang="ts">
import UserMessage from "@/components/conversation/UserMessage.vue";
import AssistantMessage from "@/components/conversation/AssistantMessage.vue";
import ToolCallMessage from "@/components/conversation/ToolCallMessage.vue";

interface UserMessage {
    type: "user";
    content: string;
    timestamp: Date;
}

interface AssistantResponse {
    type: "assistant";
    content: string;
    timestamp: Date;
}

interface ToolCall {
    type: "tool";
    toolName: string;
    arguments: string[];
    timestamp: Date;
}

type Message = UserMessage | AssistantResponse | ToolCall;

defineProps<{
    messages: Message[];
}>();
</script>

<template>
    <div class="space-y-4 p-4">
        <template v-for="(message, index) in messages" :key="index">
            <UserMessage v-if="message.type === 'user'" :message="message" />
            <AssistantMessage v-else-if="message.type === 'assistant'" :message="message" />
            <ToolCallMessage v-else-if="message.type === 'tool'" :message="message" />
        </template>

        <div v-if="messages.length === 0" class="flex items-center justify-center h-full text-muted-foreground">
            <p>No messages yet. Start a conversation below.</p>
        </div>
    </div>
</template>
