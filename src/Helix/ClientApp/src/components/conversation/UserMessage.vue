<script setup lang="ts">
import { Card, CardContent } from "@/components/ui/card";

interface UserMessage {
    type: "user";
    content: string;
    timestamp: Date;
}

defineProps<{
    message: UserMessage;
}>();
</script>

<template>
    <div class="flex justify-end">
        <Card class="max-w-[80%]">
            <CardContent class="p-3">
                <p class="text-sm">{{ message.content }}</p>
                <p class="text-xs text-muted-foreground mt-1">
                    {{ message.timestamp.toLocaleTimeString() }}
                </p>
            </CardContent>
        </Card>
    </div>
</template>
