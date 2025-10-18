<script setup lang="ts">
import { Card, CardContent } from "@/components/ui/card";

interface ToolCall {
    type: "tool";
    toolName: string;
    arguments: { [key: string]: string };
    timestamp: Date;
}

defineProps<{
    message: ToolCall;
}>();
</script>

<template>
    <div class="flex justify-center">
        <Card class="max-w-[80%]">
            <CardContent class="p-3">
                <p class="text-xs font-medium text-muted-foreground mb-1">Tool Call</p>
                <p class="text-sm font-mono">{{ message.toolName }}</p>
                <div v-if="Object.keys(message.arguments).length > 0" class="mt-2">
                    <p class="text-xs text-muted-foreground">Arguments:</p>
                    <ul class="text-xs font-mono mt-1 space-y-0.5">
                        <li v-for="(value, key) in message.arguments" :key="key" class="flex">
                            <span class="text-muted-foreground">{{ key }}:</span>
                            <span class="ml-1">{{ value }}</span>
                        </li>
                    </ul>
                </div>
                <p class="text-xs text-muted-foreground mt-2">
                    {{ message.timestamp.toLocaleTimeString() }}
                </p>
            </CardContent>
        </Card>
    </div>
</template>
