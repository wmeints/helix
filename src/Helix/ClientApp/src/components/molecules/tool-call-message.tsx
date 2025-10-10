import { useState } from "react"
import { cn } from "@/lib/utils"

interface ToolCallMessageProps {
  toolName: string
  parameters?: Record<string, any>
  timestamp?: Date
  className?: string
}

export default function ToolCallMessage({
  toolName,
  parameters,
  timestamp,
  className,
}: ToolCallMessageProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const hasParameters = parameters && Object.keys(parameters).length > 0

  return (
    <div
      className={cn(
        "group w-full border-b border-border/40 py-8 px-4",
        className,
      )}
    >
      <div className="max-w-3xl mx-auto space-y-2">
        {/* Message Type Indicator */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground/60">tool call</span>
          {timestamp && (
            <span className="text-xs text-muted-foreground/60">
              {timestamp.toLocaleTimeString([], {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          )}
        </div>

        {/* Tool Name Badge */}
        <div className="flex items-center gap-2">
          <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-amber-500/10 dark:bg-amber-500/20 border border-amber-500/20 dark:border-amber-500/30">
            <span className="text-xs font-mono font-medium text-amber-700 dark:text-amber-400">
              {toolName}
            </span>
          </div>
          {hasParameters && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors flex items-center gap-1"
            >
              {isExpanded ? "Hide" : "Show"} parameters
              <svg
                className={cn(
                  "size-3 transition-transform",
                  isExpanded && "rotate-180",
                )}
                fill="none"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="2"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path d="m6 9 6 6 6-6"></path>
              </svg>
            </button>
          )}
        </div>

        {/* Parameters Display */}
        {hasParameters && isExpanded && (
          <div className="mt-3 rounded-md bg-muted/50 dark:bg-muted/20 border border-border/50 p-3 overflow-auto">
            <pre className="text-xs font-mono text-foreground/80 whitespace-pre-wrap break-words">
              {JSON.stringify(parameters, null, 2)}
            </pre>
          </div>
        )}
      </div>
    </div>
  )
}
