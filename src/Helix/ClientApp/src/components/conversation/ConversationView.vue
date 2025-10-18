<script setup lang="ts">
import { ref, onMounted, nextTick, watch } from "vue";
import { useCodingAgent } from "@/stores/coding-agent";
import MessageList from "@/components/conversation/MessageList.vue";
import PromptForm from "@/components/conversation/PromptForm.vue";

const store = useCodingAgent();
const messagesEndRef = ref<HTMLElement | null>(null);

const scrollToBottom = () => {
    nextTick(() => {
        messagesEndRef.value?.scrollIntoView({ behavior: "smooth" });
    });
};

// Watch for new messages and scroll to bottom
watch(
    () => store.messages.length,
    () => {
        scrollToBottom();
    },
);

onMounted(() => {
    scrollToBottom();
});

const handleSubmit = async (prompt: string) => {
    await store.submitPrompt(prompt);
};
</script>

<template>
    <div class="flex flex-col h-full">
        <!-- Messages area - takes remaining space and is scrollable -->
        <div class="flex-1 overflow-y-auto">
            <MessageList :messages="store.messages" />
            <div ref="messagesEndRef" />
        </div>

        <!-- Input form - fixed at bottom -->
        <div class="border-t p-4">
            <PromptForm @submit="handleSubmit" />
        </div>
    </div>
</template>
