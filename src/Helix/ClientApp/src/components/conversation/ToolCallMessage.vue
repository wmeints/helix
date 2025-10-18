<script setup lang="ts">
import { Card, CardContent } from "@/components/ui/card";

interface ToolCall {
    type: "tool";
    toolName: string;
    arguments: string[];
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
                <div v-if="message.arguments.length > 0" class="mt-2">
                    <p class="text-xs text-muted-foreground">Arguments:</p>
                    <ul class="text-xs font-mono mt-1 space-y-0.5">
                        <li v-for="(arg, index) in message.arguments" :key="index">
                            {{ arg }}
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
