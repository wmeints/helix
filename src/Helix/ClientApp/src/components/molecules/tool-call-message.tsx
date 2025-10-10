import { cn } from "@/lib/utils";
import { useState } from "react";

interface ToolCallMessageProps {
    toolName: string;
    parameters?: Record<string, any>;
    timestamp?: Date;
    className?: string;
}

export default function ToolCallMessage({
    toolName,
    parameters,
    timestamp,
    className
}: ToolCallMessageProps) {
    const [isExpanded, setIsExpanded] = useState(false);
    const hasParameters = parameters && Object.keys(parameters).length > 0;

    return (
        <div className={cn(
            "group w-full border-b border-border/40 py-8 px-4",
            "bg-muted/30 dark:bg-muted/10",
            className
        )}>
            <div className="max-w-3xl mx-auto flex gap-4">
                {/* Tool Icon */}
                <div className="flex-shrink-0">
                    <div className="size-8 rounded-sm bg-amber-500/10 dark:bg-amber-500/20 flex items-center justify-center">
                        <svg
                            className="size-5 text-amber-600 dark:text-amber-500"
                            fill="none"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth="2"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                        >
                            <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"></path>
                        </svg>
                    </div>
                </div>

                {/* Tool Call Content */}
                <div className="flex-1 space-y-2 overflow-hidden">
                    <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold text-foreground">Tool Call</span>
                        {timestamp && (
                            <span className="text-xs text-muted-foreground">
                                {timestamp.toLocaleTimeString([], {
                                    hour: '2-digit',
                                    minute: '2-digit'
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
                                {isExpanded ? 'Hide' : 'Show'} parameters
                                <svg
                                    className={cn(
                                        "size-3 transition-transform",
                                        isExpanded && "rotate-180"
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
        </div>
    );
}