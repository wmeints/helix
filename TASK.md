# Render conversation history for the API

In the backend endpoint /api/conversations/{id} I want to return a conversation using the `ConversationInfo` class.
We should use the static method `FromConversation` on the `ConversationInfo` class to create an instance of it.
This class should have a list of messages. There are three kinds of messages:

- ToolCall
- AgentResponse
- UserPrompt

All messages are parsed from the ChatHistory property of the Conversation class instance I provide to the method.
The `ChatHistory` property contains a class `ChatHistory` which is documented here: https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.chatcompletion.chathistory?view=semantic-kernel-dotnet

The `ChatHistory` class has a property is a collection of messages in ascending order by time.
The class definition for the messages in the chat history can be found here: https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.chatmessagecontent?view=semantic-kernel-dotnet
Each message has a `Role` property which can be AuthorRole.User, AuthorRole.Assistant or AuthorRole.Tool.

## Processing a toolcall message

Parsing the ToolCall message is done by looking for messages with the role Assistant. The message should
have an item in the Items property of type `FunctionCallContent` that is documented here: https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.functioncallcontent?view=semantic-kernel-dotnet
The arguments for the tool call can be retrieved from the `FunctionCallContent` item. 

In addition to the name of the tool and the arguments, we also need to extract the response.
You can find this by looking at the message after the tool call message. It has the role Tool. It has a Content property
containing the tool response.

## Processing a regular agent response

Messages that have the role Assistant but do not have a `FunctionCallContent` item in the Items property
are regular agent responses. They should be parsed as such.

## Processing a user prompt

Messages that have the role User are user prompts. They should be parsed as such.
