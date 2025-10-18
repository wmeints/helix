import { defineStore } from "pinia";
import { ref } from "vue";

export const useConversationHistory = defineStore("conversationHistory", () => {
  const conversations = ref([]);

  async function load() {
    const response = await fetch("/api/conversations");
    const responseData = await response.json();

    conversations.value = responseData;
  }

  return { conversations, load };
});
