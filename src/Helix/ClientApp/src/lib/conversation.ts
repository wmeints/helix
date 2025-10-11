import * as signalR from "@microsoft/signalr"
import { useEffect, useRef, useState } from "react"
import type { Message } from "../components/molecules/conversation-messages"

interface UseConversationReturn {
  messages: Message[]
  submitPrompt: (prompt: string) => Promise<void>
  isConnected: boolean
}

export function useConversation(id: string): UseConversationReturn {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    // Create hub connection
    const hubUrl = import.meta.env.VITE_HUB_URL || "/hubs/conversation"
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()

    connectionRef.current = connection

    // Register hub methods
    connection.on("ReceiveUserMessage", (messageId: string, message: string) => {
      setMessages((prev) => [
        ...prev,
        {
          id: messageId,
          type: "user",
          message,
          timestamp: new Date(),
        },
      ])
    })

    connection.on("ReceiveAgentMessage", (messageId: string, message: string, isStreaming = false) => {
      setMessages((prev) => {
        // If streaming, update existing message if it exists
        if (isStreaming) {
          const existingIndex = prev.findIndex((m) => m.id === messageId)
          if (existingIndex !== -1) {
            const updated = [...prev]
            updated[existingIndex] = {
              id: messageId,
              type: "agent",
              message,
              isStreaming: true,
              timestamp: prev[existingIndex].timestamp,
            }
            return updated
          }
        }

        // Otherwise add new message
        return [
          ...prev,
          {
            id: messageId,
            type: "agent",
            message,
            isStreaming,
            timestamp: new Date(),
          },
        ]
      })
    })

    connection.on("ReceiveToolCall", (messageId: string, toolName: string, parameters?: Record<string, any>) => {
      setMessages((prev) => [
        ...prev,
        {
          id: messageId,
          type: "tool_call",
          toolName,
          parameters,
          timestamp: new Date(),
        },
      ])
    })

    connection.on("StreamComplete", (messageId: string) => {
      setMessages((prev) => {
        const updated = [...prev]
        const index = updated.findIndex((m) => m.id === messageId)
        if (index !== -1 && updated[index].type === "agent") {
          updated[index] = {
            ...updated[index],
            isStreaming: false,
          } as Message
        }
        return updated
      })
    })

    // Start connection
    connection
      .start()
      .then(() => {
        console.log("SignalR Connected")
        setIsConnected(true)
        // Join the conversation room
        connection.invoke("JoinConversation", id).catch((err) => {
          console.error("Error joining conversation:", err)
        })
      })
      .catch((err) => {
        console.error("SignalR Connection Error:", err)
        setIsConnected(false)
      })

    connection.onreconnected(() => {
      console.log("SignalR Reconnected")
      setIsConnected(true)
      // Rejoin conversation after reconnect
      connection.invoke("JoinConversation", id).catch((err) => {
        console.error("Error rejoining conversation:", err)
      })
    })

    connection.onreconnecting(() => {
      console.log("SignalR Reconnecting...")
      setIsConnected(false)
    })

    connection.onclose(() => {
      console.log("SignalR Connection Closed")
      setIsConnected(false)
    })

    // Cleanup on unmount
    return () => {
      if (connectionRef.current) {
        connectionRef.current
          .stop()
          .then(() => {
            console.log("SignalR Disconnected")
          })
          .catch((err) => {
            console.error("Error stopping SignalR connection:", err)
          })
      }
    }
  }, [id])

  const submitPrompt = async (prompt: string): Promise<void> => {
    if (!connectionRef.current || !isConnected) {
      throw new Error("Not connected to conversation hub")
    }

    try {
      await connectionRef.current.invoke("SubmitPrompt", id, prompt)
    } catch (err) {
      console.error("Error submitting prompt:", err)
      throw err
    }
  }

  return {
    messages,
    submitPrompt,
    isConnected,
  }
}
