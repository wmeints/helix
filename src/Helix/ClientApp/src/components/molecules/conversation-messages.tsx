import { useEffect, useRef } from "react"
import AgentMessage from "./agent-message"
import ToolCallMessage from "./tool-call-message"
import UserMessage from "./user-message"

// Message type definitions
export type MessageType = "user" | "agent" | "tool_call"

export interface BaseMessage {
  id: string
  timestamp?: Date
}

export interface UserMessageData extends BaseMessage {
  type: "user"
  message: string
}

export interface AgentMessageData extends BaseMessage {
  type: "agent"
  message: string
  isStreaming?: boolean
}

export interface ToolCallMessageData extends BaseMessage {
  type: "tool_call"
  toolName: string
  parameters?: Record<string, any>
}

export type Message = UserMessageData | AgentMessageData | ToolCallMessageData

interface ConversationMessagesProps {
  messages: Message[]
  autoScroll?: boolean
  className?: string
}

export default function ConversationMessages({
  messages,
  autoScroll = true,
  className,
}: ConversationMessagesProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null)

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    if (autoScroll && messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" })
    }
  }, [messages, autoScroll])

  const renderMessage = (message: Message) => {
    switch (message.type) {
      case "user":
        return (
          <UserMessage
            key={message.id}
            message={message.message}
            timestamp={message.timestamp}
          />
        )
      case "agent":
        return (
          <AgentMessage
            key={message.id}
            message={message.message}
            timestamp={message.timestamp}
            isStreaming={message.isStreaming}
          />
        )
      case "tool_call":
        return (
          <ToolCallMessage
            key={message.id}
            toolName={message.toolName}
            parameters={message.parameters}
            timestamp={message.timestamp}
          />
        )
      default:
        return null
    }
  }

  return (
    <div className={className}>
      {messages.length === 0 ? (
        <div className="flex items-center justify-center h-full p-8">
          <div className="text-center space-y-3">
            <div className="size-12 mx-auto rounded-full bg-muted/30 flex items-center justify-center">
              <svg
                className="size-6 text-muted-foreground"
                fill="none"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="2"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"></path>
              </svg>
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium text-foreground">
                No messages yet
              </p>
              <p className="text-xs text-muted-foreground">
                Start a conversation to see messages here
              </p>
            </div>
          </div>
        </div>
      ) : (
        <>
          {messages.map(renderMessage)}
          <div ref={messagesEndRef} />
        </>
      )}
    </div>
  )
}
