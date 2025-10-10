import { cn } from "@/lib/utils";

interface UserMessageProps {
    message: string;
    timestamp?: Date;
    className?: string;
}

export default function UserMessage({ message, timestamp, className }: UserMessageProps) {
    return (
        <div className={cn(
            "group w-full border-b border-border/40 py-8 px-4",
            "bg-background/50 dark:bg-background/30",
            className
        )}>
            <div className="max-w-3xl mx-auto flex gap-4">
                {/* User Avatar */}
                <div className="flex-shrink-0">
                    <div className="size-8 rounded-sm bg-primary/10 dark:bg-primary/20 flex items-center justify-center">
                        <svg
                            className="size-5 text-primary"
                            fill="none"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth="2"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                        >
                            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                            <circle cx="12" cy="7" r="4"></circle>
                        </svg>
                    </div>
                </div>

                {/* Message Content */}
                <div className="flex-1 space-y-2 overflow-hidden">
                    <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold text-foreground">You</span>
                        {timestamp && (
                            <span className="text-xs text-muted-foreground">
                                {timestamp.toLocaleTimeString([], {
                                    hour: '2-digit',
                                    minute: '2-digit'
                                })}
                            </span>
                        )}
                    </div>
                    <div className="text-sm text-foreground/90 whitespace-pre-wrap break-words">
                        {message}
                    </div>
                </div>
            </div>
        </div>
    );
}