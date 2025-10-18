# AGENTS.md - Frontend Documentation

This file provides comprehensive documentation for AI coding agents working with the Helix frontend application. It focuses on the Vue.js 3 application architecture, implementation details, and best practices.

## Project Overview

The Helix frontend is a modern web application built with Vue.js 3 and TypeScript. It provides a real-time interface for interacting with the Helix coding agent through SignalR WebSocket connections. The application uses the Composition API with the `<script setup>` syntax for cleaner, more maintainable code.

**Key Technologies:**
- Vue.js 3.5.22 with Composition API
- TypeScript 5.9
- Pinia 3.0 for state management
- Microsoft SignalR 9.0 for real-time communication
- Vite 7.1 as build tool and dev server
- Tailwind CSS 4.1 for styling
- Vitest 3.2 for unit testing
- ESLint 9 with Vue and TypeScript support
- Oxlint for fast linting
- Prettier for code formatting

**Node.js Requirements:** Node.js ^20.19.0 or >=22.12.0

## Project Structure

```
src/Helix/ClientApp/
├── src/
│   ├── assets/          # Static assets (images, global CSS)
│   │   ├── logo.svg
│   │   └── main.css     # Global Tailwind CSS imports
│   ├── components/      # Vue components (currently empty, ready for additions)
│   ├── lib/             # Utility functions and helpers (currently empty)
│   ├── stores/          # Pinia stores for state management
│   │   ├── coding-agent.ts    # Main agent interaction store
│   │   └── history.ts         # Conversation history store
│   ├── App.vue          # Root application component
│   └── main.ts          # Application entry point
├── public/              # Public static assets (served as-is)
├── dist/                # Build output directory
├── node_modules/        # NPM dependencies
├── .env.example         # Environment variables example
├── eslint.config.ts     # ESLint configuration
├── index.html           # HTML template
├── package.json         # NPM dependencies and scripts
├── tsconfig.json        # TypeScript project references
├── tsconfig.app.json    # TypeScript config for app code
├── tsconfig.node.json   # TypeScript config for Node.js scripts
├── tsconfig.vitest.json # TypeScript config for tests
├── vite.config.ts       # Vite build configuration
├── vitest.config.ts     # Vitest test configuration
└── README.md            # User-facing documentation
```

## Architecture Overview

### Application Entry Point

**File:** `src/main.ts`

The application entry point initializes Vue 3, sets up Pinia for state management, and mounts the root component:

```typescript
import "./assets/main.css";
import { createApp } from "vue";
import { createPinia } from "pinia";
import App from "./App.vue";

const app = createApp(App);
app.use(createPinia());
app.mount("#app");
```

### State Management with Pinia

The application uses Pinia stores to manage application state. Stores are located in `src/stores/`.

#### Coding Agent Store

**File:** `src/stores/coding-agent.ts`

The main store for interacting with the coding agent backend via SignalR.

**Key Features:**
- **SignalR Connection Management:** Establishes and maintains WebSocket connection to `/hubs/coding-agent`
- **Automatic Reconnection:** Uses SignalR's built-in automatic reconnect functionality
- **Message History:** Maintains a reactive array of conversation messages
- **Conversation Management:** Creates and tracks conversation IDs

**Store State:**
- `messages` - Array of conversation messages (user, assistant, tool calls)
- `connection` - SignalR HubConnection instance
- `isConnected` - Boolean connection status
- `conversationId` - Current conversation ID (auto-generated)
- `history` - Array of conversation info objects

**Store Actions:**

1. **`connect()`** - Establishes SignalR connection
   - Creates HubConnection with automatic reconnect
   - Sets up event handlers for `ReceiveAgentResponse` and `ReceiveToolCall`
   - Manages connection state

2. **`submitPrompt(prompt: string)`** - Sends user prompt to agent
   - Auto-generates conversation ID if needed
   - Adds user message to local state
   - Invokes `SubmitPrompt` hub method

3. **`loadHistory()`** - Fetches conversation history from API
   - Calls `/api/conversations` endpoint

4. **`loadConversation(id: string)`** - Loads specific conversation
   - Calls `/api/conversations/{id}` endpoint
   - Updates current conversation state

5. **`reset()`** - Clears current conversation state

**Message Types:**

```typescript
interface UserMessage {
  type: 'user'
  content: string
  timestamp: Date
}

interface AssistantResponse {
  type: 'assistant'
  content: string
  timestamp: Date
}

interface ToolCall {
  type: 'tool'
  toolName: string
  arguments: string[]
  timestamp: Date
}
```

#### Conversation History Store

**File:** `src/stores/history.ts`

A simplified store for managing conversation history lists.

**Store State:**
- `conversations` - Array of conversation summaries

**Store Actions:**
- `load()` - Fetches all conversations from `/api/conversations`

### SignalR Integration

The application communicates with the backend using SignalR WebSockets for real-time updates.

**Hub URL:** `/hubs/coding-agent`

**Server Methods (called by frontend):**
- `SubmitPrompt(conversationId, prompt)` - Submit new prompt to agent
- `ApproveToolCall(conversationId, toolCallId)` - Approve a pending tool call
- `DeclineToolCall(conversationId, toolCallId)` - Decline a pending tool call

**Client Methods (called by backend):**
- `ReceiveAgentResponse(content, timestamp)` - Receive agent text response
- `ReceiveToolCall(toolName, args, timestamp)` - Receive tool call notification

### Vite Configuration

**File:** `vite.config.ts`

Vite is configured with:
- **Vue Plugin:** Enables Vue 3 single-file component support
- **Vue DevTools:** Development-only plugin for Vue debugging
- **Tailwind CSS:** Vite plugin for Tailwind v4
- **Path Aliases:** `@/` maps to `./src/`
- **Proxy Configuration:** 
  - `/hubs/*` → `http://localhost:5000` (SignalR)
  - `/api/*` → `http://localhost:5000` (REST API)

### TypeScript Configuration

The project uses TypeScript project references for better build performance:

- **`tsconfig.json`** - Root config with project references
- **`tsconfig.app.json`** - App source code configuration
  - Extends `@vue/tsconfig/tsconfig.dom.json`
  - Includes `src/**/*` and `*.vue` files
  - Excludes test files
  - Configures `@/*` path alias
- **`tsconfig.node.json`** - Node.js scripts (Vite config, etc.)
- **`tsconfig.vitest.json`** - Test files configuration

## Vue.js Best Practices

### Component Structure

When creating new Vue components, follow these best practices:

#### 1. Use Composition API with `<script setup>`

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'

// Props
interface Props {
  title: string
  count?: number
}

const props = withDefaults(defineProps<Props>(), {
  count: 0
})

// Emits
const emit = defineEmits<{
  update: [value: number]
  delete: []
}>()

// State
const localCount = ref(props.count)

// Computed
const doubleCount = computed(() => localCount.value * 2)

// Methods
function increment() {
  localCount.value++
  emit('update', localCount.value)
}
</script>

<template>
  <div>
    <h2>{{ title }}</h2>
    <p>Count: {{ localCount }} (Double: {{ doubleCount }})</p>
    <button @click="increment">Increment</button>
  </div>
</template>

<style scoped>
/* Component-specific styles */
</style>
```

#### 2. Component Organization

Order your component sections consistently:
1. `<script setup lang="ts">` - Logic first
2. `<template>` - Template second
3. `<style scoped>` - Styles last

#### 3. TypeScript Types

Always define types for props, emits, and complex data structures:

```typescript
// Define interfaces at the top of the script
interface User {
  id: string
  name: string
  email: string
}

// Use typed refs
const users = ref<User[]>([])
const currentUser = ref<User | null>(null)
```

#### 4. Composables

Extract reusable logic into composables (place in `src/lib/composables/`):

```typescript
// useWebSocket.ts
import { ref, onUnmounted } from 'vue'

export function useWebSocket(url: string) {
  const isConnected = ref(false)
  const connection = ref<WebSocket | null>(null)

  function connect() {
    connection.value = new WebSocket(url)
    connection.value.onopen = () => { isConnected.value = true }
    connection.value.onclose = () => { isConnected.value = false }
  }

  onUnmounted(() => {
    connection.value?.close()
  })

  return { isConnected, connect, connection }
}
```

#### 5. Component Naming

- Use PascalCase for component files: `UserProfile.vue`, `ChatMessage.vue`
- Use multi-word names to avoid conflicts with HTML elements
- Be descriptive: `ConversationList.vue` instead of `List.vue`

### Pinia Store Best Practices

#### 1. Store Structure

```typescript
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useUserStore = defineStore('user', () => {
  // State (use ref)
  const currentUser = ref<User | null>(null)
  const users = ref<User[]>([])

  // Getters (use computed)
  const userCount = computed(() => users.value.length)
  const isAuthenticated = computed(() => currentUser.value !== null)

  // Actions (use functions)
  async function fetchUsers() {
    const response = await fetch('/api/users')
    users.value = await response.json()
  }

  function setCurrentUser(user: User) {
    currentUser.value = user
  }

  return {
    // State
    currentUser,
    users,
    // Getters
    userCount,
    isAuthenticated,
    // Actions
    fetchUsers,
    setCurrentUser
  }
})
```

#### 2. Store Organization

- One store per domain/feature
- Keep stores focused and single-purpose
- Use composition to combine stores when needed

#### 3. Accessing Stores

```typescript
// In components
import { useCodingAgent } from '@/stores/coding-agent'

const agentStore = useCodingAgent()

// Access state
console.log(agentStore.messages)

// Call actions
agentStore.submitPrompt('Hello')
```

### Reactivity Best Practices

#### 1. Use `ref` for Primitives, `reactive` for Objects

```typescript
// Good
const count = ref(0)
const user = reactive({ name: 'John', age: 30 })

// Also good (preferred for complex objects)
const user = ref<User>({ name: 'John', age: 30 })
```

#### 2. Destructuring Reactive Objects

```typescript
import { toRefs } from 'vue'

const user = reactive({ name: 'John', age: 30 })

// Don't destructure directly (loses reactivity)
// const { name } = user

// Use toRefs instead
const { name, age } = toRefs(user)
```

#### 3. Watch and WatchEffect

```typescript
import { watch, watchEffect } from 'vue'

// Watch specific sources
watch(
  () => agentStore.messages,
  (newMessages, oldMessages) => {
    console.log('Messages changed', newMessages)
  },
  { deep: true }
)

// Watch effect for side effects
watchEffect(() => {
  if (agentStore.isConnected) {
    console.log('Connected!')
  }
})
```

### Template Best Practices

#### 1. Use v-for with :key

```vue
<template>
  <div v-for="message in messages" :key="message.id">
    {{ message.content }}
  </div>
</template>
```

#### 2. Conditional Rendering

```vue
<template>
  <!-- Use v-if for conditional mounting -->
  <div v-if="isLoading">Loading...</div>
  <div v-else-if="error">Error: {{ error }}</div>
  <div v-else>{{ content }}</div>

  <!-- Use v-show for frequent toggling -->
  <div v-show="isVisible">Toggle me!</div>
</template>
```

#### 3. Event Handling

```vue
<template>
  <!-- Inline handlers -->
  <button @click="count++">Increment</button>

  <!-- Method handlers -->
  <button @click="handleClick">Click Me</button>

  <!-- With modifiers -->
  <form @submit.prevent="handleSubmit">
    <input @keyup.enter="handleEnter" />
  </form>
</template>
```

### Styling Best Practices

#### 1. Scoped Styles

Always use `scoped` attribute for component-specific styles:

```vue
<style scoped>
.button {
  padding: 0.5rem 1rem;
  border-radius: 0.25rem;
}
</style>
```

#### 2. Tailwind CSS Usage

The project uses Tailwind CSS 4.1. Use utility classes in templates:

```vue
<template>
  <div class="flex items-center justify-between p-4 bg-gray-100 rounded-lg">
    <h2 class="text-xl font-bold text-gray-900">Title</h2>
    <button class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-700">
      Click Me
    </button>
  </div>
</template>
```

#### 3. Global Styles

Global styles are defined in `src/assets/main.css`:

```css
@import "tailwindcss";
```

## Build and Development Commands

### Installation

```bash
# Install dependencies
npm install

# Or with specific Node version
nvm use 20
npm install
```

### Development

```bash
# Start development server with hot reload
npm run dev

# Server starts at http://localhost:5173/
# Proxies /hubs and /api requests to http://localhost:5000
```

**Important:** The backend must be running on `http://localhost:5000` for the proxy to work.

### Building for Production

```bash
# Type-check and build
npm run build

# This runs:
# 1. npm run type-check (runs vue-tsc --build)
# 2. npm run build-only (runs vite build)

# Output is in the dist/ directory
```

### Type Checking

```bash
# Run TypeScript compiler for type checking (no emit)
npm run type-check

# This runs: vue-tsc --build
```

### Testing

```bash
# Run unit tests
npm run test:unit

# Run tests in watch mode
npm run test:unit -- --watch

# Run tests with coverage
npm run test:unit -- --coverage
```

**Test Framework:** Vitest with jsdom environment

**Test Location:** Place tests in `src/**/__tests__/` directories

**Example Test:**

```typescript
// src/components/__tests__/MyComponent.spec.ts
import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import MyComponent from '../MyComponent.vue'

describe('MyComponent', () => {
  it('renders properly', () => {
    const wrapper = mount(MyComponent, {
      props: { title: 'Hello' }
    })
    expect(wrapper.text()).toContain('Hello')
  })
})
```

### Linting

```bash
# Run all linters (oxlint + eslint)
npm run lint

# This runs sequentially:
# 1. npm run lint:oxlint
# 2. npm run lint:eslint
```

**Oxlint (Fast):**
```bash
# Run oxlint with auto-fix
npm run lint:oxlint

# Focuses on correctness rules only
# Very fast, runs first
```

**ESLint (Comprehensive):**
```bash
# Run ESLint with auto-fix
npm run lint:eslint

# Runs Vue, TypeScript, and code style checks
```

### Code Formatting

```bash
# Format code with Prettier
npm run format

# This formats all files in src/ directory
```

**Prettier Configuration:**
- Uses `@prettier/plugin-oxc` for faster parsing
- Configured to skip formatting in ESLint (avoids conflicts)

### Preview Production Build

```bash
# Build and preview
npm run build
npm run preview

# Preview server starts at http://localhost:4173/
```

## ESLint Configuration

**File:** `eslint.config.ts`

The project uses ESLint 9 with flat config format.

**Enabled Plugins:**
- `eslint-plugin-vue` - Vue.js specific rules
- `@vue/eslint-config-typescript` - TypeScript support for Vue
- `@vitest/eslint-plugin` - Vitest testing rules
- `eslint-plugin-oxlint` - Integrates with oxlint
- `@vue/eslint-config-prettier` - Disables formatting rules

**Configuration:**
```typescript
export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue}'],
  },
  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),
  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,
  {
    ...pluginVitest.configs.recommended,
    files: ['src/**/__tests__/*'],
  },
  ...pluginOxlint.configs['flat/recommended'],
  skipFormatting,
)
```

## Common Development Tasks

### Adding a New Component

1. Create component file in `src/components/`:
```bash
touch src/components/ChatMessage.vue
```

2. Define component with TypeScript:
```vue
<script setup lang="ts">
interface Props {
  content: string
  timestamp: Date
}

const props = defineProps<Props>()
</script>

<template>
  <div class="chat-message">
    <p>{{ content }}</p>
    <time>{{ timestamp.toLocaleString() }}</time>
  </div>
</template>

<style scoped>
.chat-message {
  padding: 1rem;
  margin-bottom: 0.5rem;
}
</style>
```

3. Import and use in parent component:
```vue
<script setup lang="ts">
import ChatMessage from '@/components/ChatMessage.vue'
</script>

<template>
  <ChatMessage 
    content="Hello" 
    :timestamp="new Date()" 
  />
</template>
```

### Creating a New Store

1. Create store file in `src/stores/`:
```bash
touch src/stores/user.ts
```

2. Define store:
```typescript
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useUserStore = defineStore('user', () => {
  const name = ref('')
  const email = ref('')

  function setUser(newName: string, newEmail: string) {
    name.value = newName
    email.value = newEmail
  }

  return { name, email, setUser }
})
```

3. Use in component:
```vue
<script setup lang="ts">
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
</script>
```

### Adding a New API Endpoint

1. If using a store, add the API call:
```typescript
async function fetchData() {
  const response = await fetch('/api/data')
  if (!response.ok) {
    throw new Error('Failed to fetch data')
  }
  data.value = await response.json()
}
```

2. The Vite proxy will forward `/api/*` to `http://localhost:5000`

### Adding Environment Variables

1. Add to `.env.example`:
```bash
VITE_API_URL=http://localhost:5000
```

2. Access in code:
```typescript
const apiUrl = import.meta.env.VITE_API_URL
```

**Important:** Environment variables must be prefixed with `VITE_` to be exposed to the client.

## Troubleshooting

### Development Server Won't Start

**Issue:** Port 5173 already in use

**Solution:**
```bash
# Kill process using port 5173
npx kill-port 5173

# Or specify different port
vite --port 3000
```

### SignalR Connection Fails

**Issue:** WebSocket connection to `/hubs/coding-agent` fails

**Checklist:**
- Backend server is running on `http://localhost:5000`
- Vite proxy is configured correctly in `vite.config.ts`
- Check browser console for connection errors
- Verify SignalR hub endpoint matches backend

### TypeScript Errors in `.vue` Files

**Issue:** VS Code shows TypeScript errors in templates

**Solution:**
- Install Volar extension (Vue.volar)
- Disable Vetur if installed
- Restart TypeScript server: Cmd/Ctrl + Shift + P → "TypeScript: Restart TS Server"

### Build Fails

**Issue:** `npm run build` fails

**Common Causes:**
- TypeScript errors: Run `npm run type-check` to see details
- Missing dependencies: Run `npm install`
- Node version mismatch: Use Node 20.x or 22.x

### Tests Fail

**Issue:** `npm run test:unit` fails

**Solutions:**
- Check test file syntax
- Ensure test dependencies are installed
- Review Vitest configuration in `vitest.config.ts`
- Run with verbose output: `npm run test:unit -- --reporter=verbose`

### Linter Errors

**Issue:** ESLint or oxlint reports errors

**Solutions:**
```bash
# Auto-fix what's possible
npm run lint

# Check specific rules
npm run lint:eslint -- --debug

# Disable specific rule in file
/* eslint-disable vue/no-unused-vars */
```

## IDE Setup

### Recommended VS Code Extensions

- **Vue (Official)** - `Vue.volar` - Vue language support
- **TypeScript Vue Plugin** - Included with Volar
- **ESLint** - `dbaeumer.vscode-eslint` - ESLint integration
- **Prettier** - `esbenp.prettier-vscode` - Code formatting
- **Tailwind CSS IntelliSense** - `bradlc.vscode-tailwindcss` - Tailwind autocomplete

### VS Code Settings

Add to `.vscode/settings.json`:

```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "[vue]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "eslint.validate": [
    "javascript",
    "typescript",
    "vue"
  ],
  "typescript.tsdk": "node_modules/typescript/lib"
}
```

### Browser DevTools

**Chromium Browsers:**
- Install [Vue.js devtools](https://chromewebstore.google.com/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd)
- Enable Custom Object Formatters in DevTools settings

**Firefox:**
- Install [Vue.js devtools](https://addons.mozilla.org/en-US/firefox/addon/vue-js-devtools/)
- Enable Custom Object Formatters in DevTools settings

## Additional Resources

- **Vue.js Documentation:** https://vuejs.org/guide/
- **Pinia Documentation:** https://pinia.vuejs.org/
- **Vite Documentation:** https://vite.dev/guide/
- **TypeScript Vue:** https://vuejs.org/guide/typescript/overview.html
- **Tailwind CSS:** https://tailwindcss.com/docs
- **SignalR JavaScript Client:** https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- **Vitest Documentation:** https://vitest.dev/guide/

## Contributing Guidelines

When making changes to the frontend:

1. **Follow Vue.js Best Practices:** Use Composition API with `<script setup>`
2. **Type Everything:** Use TypeScript for all new code
3. **Test Your Changes:** Write unit tests for new components and stores
4. **Lint Before Committing:** Run `npm run lint` to catch issues
5. **Format Code:** Use `npm run format` to maintain consistent style
6. **Update Documentation:** Update this file if architecture changes
7. **Keep Components Small:** Break large components into smaller, reusable ones
8. **Use Pinia for State:** Don't use component-level state for shared data
9. **Scoped Styles:** Always use `scoped` attribute in component styles
10. **Meaningful Names:** Use descriptive names for components, variables, and functions

---

**Last Updated:** This documentation reflects the project state as of Vue 3.5.22, Vite 7.1, and Tailwind CSS 4.1.
