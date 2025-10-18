import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { defineStore } from "pinia";
import { ref } from "vue";

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

interface ConversationInfo {
  id: string;
  description: string;
}

type Message = UserMessage | AssistantResponse | ToolCall;

export const useCodingAgent = defineStore("coding-agent", () => {
  const messages = ref<Message[]>([]);
  const connection = ref<HubConnection | null>(null);
  const isConnected = ref(false);
  const conversationId = ref<string | null>(null);
  const history = ref<ConversationInfo[]>([]);

  function connect() {
    connection.value = new HubConnectionBuilder()
      .withUrl("/hubs/coding-agent")
      .withAutomaticReconnect()
      .build();

    connection.value.onreconnected(() => {
      isConnected.value = true;
      console.log("WebSocket reconnected.");
    });

    connection.value.onclose(() => {
      isConnected.value = false;
      console.log("WebSocket disconnected.");
    });

    connection.value.on(
      "ReceiveAgentResponse",
      (content: string, timestamp: string) => {
        messages.value.push({
          type: "assistant",
          content,
          timestamp: new Date(timestamp),
        });
      },
    );

    connection.value.on(
      "ReceiveToolCall",
      (toolName: string, args: string[], timestamp: string) => {
        messages.value.push({
          type: "tool",
          toolName,
          arguments: args,
          timestamp: new Date(timestamp),
        });
      },
    );

    connection.value.start();
    isConnected.value = true;
  }

  async function submitPrompt(prompt: string) {
    if (!isConnected.value) {
      console.error("WebSocket is not connected.");
      return;
    }

    // Automatically generate a conversation ID if it doesn't exist.
    if (!conversationId.value) {
      const id = crypto.randomUUID();
      const timestamp = new Date();
      const description = `Conversation ${timestamp.toLocaleDateString()} ${timestamp.toLocaleTimeString()}`;

      conversationId.value = id;

      history.value.push({
        id,
        description,
      });
    }

    messages.value.push({
      type: "user",
      content: prompt,
      timestamp: new Date(),
    });

    await connection.value?.invoke("SubmitPrompt", [
      conversationId.value,
      prompt,
    ]);
  }

  async function loadHistory() {
    const response = await fetch("/api/conversations");
    const responseData = await response.json();

    history.value = responseData;
  }

  async function loadConversation(id: string) {
    const response = await fetch(`/api/conversations/${id}`);
    const responseData = await response.json();

    conversationId.value = responseData.id;
    messages.value = responseData.messages;
  }

  function reset() {
    messages.value = [];
    conversationId.value = null;
  }

  return {
    connection,
    isConnected,
    messages,
    conversationId,
    history,
    connect,
    submitPrompt,
    reset,
    loadHistory,
    loadConversation,
  };
});
