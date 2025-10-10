import ConversationInput from "../molecules/conversation-input";
import ConversationMessages, { type Message } from "../molecules/conversation-messages";

export default function AppConversation() {
    function onSubmitPrompt({ prompt }: { prompt: string }) {
        console.log("Prompt submitted:", prompt);
    }

    // Sample messages for demonstration
    const sampleMessages: Message[] = [
        {
            id: '1',
            type: 'user',
            message: 'Hello! Can you help me understand how to use the Anthropic API?',
            timestamp: new Date(Date.now() - 600000)
        },
        {
            id: '2',
            type: 'agent',
            message: 'Of course! I\'d be happy to help you understand the Anthropic API. The API allows you to integrate Claude into your applications. What specific aspect would you like to know more about?',
            timestamp: new Date(Date.now() - 590000)
        },
        {
            id: '3',
            type: 'user',
            message: 'How do I make a basic API call?',
            timestamp: new Date(Date.now() - 580000)
        },
        {
            id: '4',
            type: 'tool_call',
            toolName: 'search_documentation',
            parameters: {
                query: 'Anthropic API basic call example',
                source: 'official_docs',
                maxResults: 5
            },
            timestamp: new Date(Date.now() - 570000)
        },
        {
            id: '5',
            type: 'agent',
            message: 'Here\'s a basic example of making an API call to Claude:\n\n```python\nimport anthropic\n\nclient = anthropic.Anthropic(\n    api_key="your-api-key"\n)\n\nmessage = client.messages.create(\n    model="claude-3-5-sonnet-20241022",\n    max_tokens=1024,\n    messages=[\n        {"role": "user", "content": "Hello, Claude!"}\n    ]\n)\n\nprint(message.content)\n```\n\nThis creates a simple conversation with Claude.',
            timestamp: new Date(Date.now() - 560000)
        },
        {
            id: '6',
            type: 'user',
            message: 'That\'s helpful! What about streaming responses?',
            timestamp: new Date(Date.now() - 550000)
        },
        {
            id: '7',
            type: 'tool_call',
            toolName: 'code_search',
            parameters: {
                query: 'streaming API example',
                language: 'python'
            },
            timestamp: new Date(Date.now() - 540000)
        },
        {
            id: '8',
            type: 'agent',
            message: 'For streaming responses, you can use the stream parameter. This allows you to receive the response incrementally as it\'s generated, which is great for real-time applications and better user experience.',
            timestamp: new Date(Date.now() - 530000)
        },
        {
            id: '9',
            type: 'user',
            message: 'Perfect! One more question - what are the rate limits?',
            timestamp: new Date(Date.now() - 520000)
        },
        {
            id: '10',
            type: 'agent',
            message: 'Rate limits depend on your plan tier. Generally, they\'re measured in requests per minute (RPM) and tokens per minute (TPM). You can check your specific limits in the Anthropic Console dashboard. The API will return a 429 status code if you exceed your limits.',
            timestamp: new Date(Date.now() - 510000)
        }
    ];

    return (
        <div className="flex flex-col h-screen">
            <div className="flex-1 overflow-y-auto">
                <ConversationMessages messages={sampleMessages} />
            </div>
            <div className="bg-background">
                <div className="mx-auto lg:max-w-7xl w-full">
                    <ConversationInput onSubmitPrompt={onSubmitPrompt} />
                </div>
            </div>
        </div>
    )
}