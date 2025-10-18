<script setup lang="ts">
import { ref } from "vue";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const emit = defineEmits<{
    submit: [prompt: string];
}>();

const prompt = ref("");
const isSubmitting = ref(false);

const handleSubmit = async () => {
    if (!prompt.value.trim() || isSubmitting.value) {
        return;
    }

    isSubmitting.value = true;

    try {
        emit("submit", prompt.value);
        prompt.value = "";
    } finally {
        isSubmitting.value = false;
    }
};

const handleKeydown = (event: KeyboardEvent) => {
    if (event.key === "Enter" && !event.shiftKey) {
        event.preventDefault();
        handleSubmit();
    }
};
</script>

<template>
    <form @submit.prevent="handleSubmit" class="flex gap-2">
        <Input v-model="prompt" placeholder="Type your message..." :disabled="isSubmitting" @keydown="handleKeydown"
            class="flex-1" />
        <Button type="submit" :disabled="!prompt.trim() || isSubmitting">
            Send
        </Button>
    </form>
</template>
