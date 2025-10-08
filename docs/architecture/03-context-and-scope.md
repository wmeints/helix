# Context and scope

This section covers the context and scope for the Helix coding agent.

## Business scope

This agent runs independently on the computer of the user and is integrated with
the LLM provider of their choosing through the use of Semantic Kernel. We don't
require the user to run any API in the cloud or on their own servers.

We require no authentication, because we ask the user to bring their own API
Key for the LLM that they want to use.

## Technical context

The agent is built as a console application with a limited user interface that
allows the user to communicate with the agent. The agent integrates with a local
copy of a codebase to do its work. It connects to an LLM provider of the user's
choosing via HTTPS.

We don't use any semantic indexing in the agent. Instead we rely on smart 
shell integration to find the information we need to run the agent.
