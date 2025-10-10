import { cn } from "@/lib/utils";
import { useState } from "react";

interface AgentMessageProps {
    message: string;
    timestamp?: Date;
    isStreaming?: boolean;
    className?: string;
}

export default function AgentMessage({
    message,
    timestamp,
    isStreaming = false,
    className
}: AgentMessageProps) {
    const [isCopied, setIsCopied] = useState(false);

    const handleCopy = async () => {
        await navigator.clipboard.writeText(message);
        setIsCopied(true);
        setTimeout(() => setIsCopied(false), 2000);
    };

    return (
        <div className={cn(
            "group w-full border-b border-border/40 py-8 px-4",
            "bg-muted/20 dark:bg-muted/5",
            className
        )}>
            <div className="max-w-3xl mx-auto flex gap-4">
                {/* Agent Avatar */}
                <div className="flex-shrink-0">
                    <div className="size-8 rounded-sm bg-gradient-to-br from-purple-500/20 to-blue-500/20 dark:from-purple-500/30 dark:to-blue-500/30 flex items-center justify-center border border-purple-500/20 dark:border-purple-500/30">
                        <svg
                            className="size-5 text-purple-600 dark:text-purple-400"
                            fill="none"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth="2"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                        >
                            <path d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.456 2.456L21.75 6l-1.035.259a3.375 3.375 0 0 0-2.456 2.456Z"></path>
                        </svg>
                    </div>
                </div>

                {/* Message Content */}
                <div className="flex-1 space-y-2 overflow-hidden min-w-0">
                    <div className="flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                            <span className="text-sm font-semibold text-foreground">Assistant</span>
                            {timestamp && (
                                <span className="text-xs text-muted-foreground">
                                    {timestamp.toLocaleTimeString([], {
                                        hour: '2-digit',
                                        minute: '2-digit'
                                    })}
                                </span>
                            )}
                            {isStreaming && (
                                <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
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
                                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                        </svg>
                                    )}
                                </button>
                            </div>
                        )}
                    </div>

                    {/* Message Text */}
                    <div className="prose prose-sm dark:prose-invert max-w-none">
                        <div className="text-sm text-foreground/90 whitespace-pre-wrap break-words leading-relaxed">
                            {message}
                            {isStreaming && (
                                <span className="inline-block w-1.5 h-4 ml-0.5 bg-foreground/70 animate-pulse"></span>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
