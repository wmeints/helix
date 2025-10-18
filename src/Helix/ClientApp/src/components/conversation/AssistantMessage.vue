<script setup lang="ts">
import { Card, CardContent } from "@/components/ui/card";

interface AssistantResponse {
    type: "assistant";
    content: string;
    timestamp: Date;
}

defineProps<{
    message: AssistantResponse;
}>();
</script>

<template>
    <div class="flex justify-start">
        <Card class="max-w-[80%]">
            <CardContent class="p-3">
                <p class="text-sm whitespace-pre-wrap">{{ message.content }}</p>
                <p class="text-xs text-muted-foreground mt-1">
                    {{ message.timestamp.toLocaleTimeString() }}
                </p>
            </CardContent>
        </Card>
    </div>
</template>
