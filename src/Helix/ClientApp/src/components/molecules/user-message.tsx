import { cn } from "@/lib/utils"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"

interface UserMessageProps {
  message: string
  timestamp?: Date
  className?: string
}

export default function UserMessage({
  message,
  timestamp,
  className,
}: UserMessageProps) {
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
          <span className="text-xs text-muted-foreground/60">prompt</span>
          {timestamp && (
            <span className="text-xs text-muted-foreground/60">
              {timestamp.toLocaleTimeString([], {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          )}
        </div>

        {/* Message Content */}
        <div className="prose prose-sm dark:prose-invert max-w-none prose-pre:bg-muted prose-pre:border prose-pre:border-border">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{message}</ReactMarkdown>
        </div>
      </div>
    </div>
  )
}
