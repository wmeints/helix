import { useConversation } from "../../lib/conversation"
import ConversationInput from "../molecules/conversation-input"
import ConversationMessages from "../molecules/conversation-messages"

export default function AppConversation() {
  // Hard-coded conversation ID for now - will come from application state in the future
  const { messages, submitPrompt } = useConversation("default-conversation")

  async function onSubmitPrompt({ prompt }: { prompt: string }) {
    try {
      await submitPrompt(prompt)
    } catch (error) {
      console.error("Failed to submit prompt:", error)
    }
  }

  return (
    <div className="flex flex-col h-screen">
      <div className="flex-1 overflow-y-auto">
        <ConversationMessages messages={messages} />
      </div>
      <div className="bg-background">
        <div className="mx-auto lg:max-w-7xl w-full">
          <ConversationInput onSubmitPrompt={onSubmitPrompt} />
        </div>
      </div>
    </div>
  )
}
