import { useState } from "react"
import { cn } from "@/lib/utils"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"

interface AgentMessageProps {
  message: string
  timestamp?: Date
  isStreaming?: boolean
  className?: string
}

export default function AgentMessage({
  message,
  timestamp,
  isStreaming = false,
  className,
}: AgentMessageProps) {
  const [isCopied, setIsCopied] = useState(false)

  const handleCopy = async () => {
    await navigator.clipboard.writeText(message)
    setIsCopied(true)
    setTimeout(() => setIsCopied(false), 2000)
  }

  return (
    <div
      className={cn(
        "group w-full border-b border-border/40 py-8 px-4",
        className,
      )}
    >
      <div className="max-w-3xl mx-auto space-y-2">
        {/* Message Type Indicator */}
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground/60">
              assistant response
            </span>
            {timestamp && (
              <span className="text-xs text-muted-foreground/60">
                {timestamp.toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </span>
            )}
            {isStreaming && (
              <span className="flex items-center gap-1.5 text-xs text-muted-foreground/60">
                <span className="relative flex size-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-purple-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full size-2 bg-purple-500"></span>
                </span>
                Typing...
              </span>
            )}
          </div>

          {/* Action Buttons */}
          {!isStreaming && message && (
            <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button
                onClick={handleCopy}
                className="p-1.5 rounded hover:bg-muted/50 dark:hover:bg-muted/30 transition-colors"
                title="Copy message"
              >
                {isCopied ? (
                  <svg
                    className="size-4 text-green-600 dark:text-green-500"
                    fill="none"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path d="M5 13l4 4L19 7"></path>
                  </svg>
                ) : (
                  <svg
                    className="size-4 text-muted-foreground hover:text-foreground transition-colors"
                    fill="none"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <rect
                      x="9"
                      y="9"
                      width="13"
                      height="13"
                      rx="2"
                      ry="2"
                    ></rect>
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                  </svg>
                )}
              </button>
            </div>
          )}
        </div>

        {/* Message Text */}
        <div className="prose prose-sm dark:prose-invert max-w-none prose-pre:bg-muted prose-pre:border prose-pre:border-border">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{message}</ReactMarkdown>
          {isStreaming && (
            <span className="inline-block w-1.5 h-4 ml-0.5 bg-foreground/70 animate-pulse"></span>
          )}
        </div>
      </div>
    </div>
  )
}
