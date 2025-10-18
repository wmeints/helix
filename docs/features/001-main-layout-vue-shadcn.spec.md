# Main Layout with Vue-ShadCN Components

## Overview

This feature implements the main layout for the Helix frontend application 
using vue-shadcn components. The layout will consist of a collapsible sidebar 
containing conversation history and a "New Conversation" button, along with a 
main content area. This provides the foundation for the user interface while 
establishing a consistent design system using shadcn-vue components.

## Requirements

### Functional Requirements

- **FR1**: Implement a responsive main layout with sidebar and main content area
- **FR2**: Create a dedicated ConversationSidebar component that displays conversation history
- **FR3**: Add a "New Conversation" button at the top of the sidebar
- **FR4**: Integrate with existing Pinia stores for conversation data
- **FR5**: Support sidebar collapse/expand functionality
- **FR6**: Maintain responsive design for mobile and desktop
- **FR7**: Use vue-shadcn components for consistent styling

### Non-Functional Requirements

- **NFR1**: Follow Vue 3 Composition API best practices with `<script setup>` syntax
- **NFR2**: Maintain TypeScript type safety throughout
- **NFR3**: Ensure accessibility compliance (keyboard navigation, ARIA labels)
- **NFR4**: Support dark/light theme switching via shadcn theming
- **NFR5**: Optimize for performance with proper component lazy loading

### Technical Requirements

- **TR1**: Install and configure vue-shadcn components
- **TR2**: Update Tailwind CSS configuration for shadcn compatibility
- **TR3**: Create reusable layout components
- **TR4**: Integrate with existing SignalR connection for real-time updates
- **TR5**: Maintain backward compatibility with existing stores

## Design

### Architecture Overview
The main layout will follow a standard sidebar + main content pattern:

```
┌───────────────────────────────────────────┐
│ MainLayout.vue                            │
│ ┌─────────────┬─────────────────────────┐ │
│ │             │                         │ │
│ │ Conversation│   Main Content Area     │ │
│ │ Sidebar     │   (slot for future      │ │
│ │             │    implementation)      │ │
│ │             │                         │ │
│ └─────────────┴─────────────────────────┘ │
└───────────────────────────────────────────┘
```

### Component Structure
```
src/components/
├── layout/
│   ├── MainLayout.vue          # Root layout component
│   └── ConversationSidebar.vue # Sidebar with conversation history
└── ui/                         # shadcn-vue components (auto-generated)
    ├── sidebar/
    ├── button/
    ├── card/
    └── ...
```

### Key Components

#### MainLayout.vue
- Wraps the entire application layout
- Uses `SidebarProvider` for sidebar state management
- Contains sidebar and main content area
- Handles responsive breakpoints

#### ConversationSidebar.vue
- Displays list of previous conversations
- "New Conversation" button at the top
- Uses shadcn Sidebar components
- Integrates with history store
- Shows conversation titles with timestamps

### Data Flow

1. **Conversation History**: Retrieved from `history` store and displayed in sidebar
2. **New Conversation**: Button triggers creation of new conversation via coding-agent store
3. **Sidebar State**: Managed by shadcn's `SidebarProvider` with persistence
4. **Real-time Updates**: Existing SignalR connection updates conversation list

### Styling Strategy
- Use shadcn-vue's built-in CSS variables for theming
- Leverage Tailwind CSS classes for layout and spacing
- Implement responsive design with Tailwind breakpoints
- Support both light and dark themes

## Implementation Steps

### Phase 1: Setup and Configuration
1. **Install shadcn-vue CLI and dependencies**
   ```bash
   cd src/Helix/ClientApp
   pnpm dlx shadcn-vue@latest init
   ```

2. **Update Tailwind configuration for shadcn compatibility**
   - Update `src/assets/main.css` with shadcn CSS imports
   - Configure CSS variables for sidebar theming

3. **Install required shadcn components**
   ```bash
   pnpm dlx shadcn-vue@latest add sidebar button card
   ```

4. **Update TypeScript configuration**
   - Add path mapping for `@/components/ui/*` imports
   - Update `vite.config.ts` for proper path resolution

### Phase 2: Layout Components
1. **Create MainLayout.vue component**
   - Implement basic sidebar + main content structure
   - Add `SidebarProvider` wrapper
   - Include `SidebarTrigger` for mobile/desktop toggle
   - Add slot for main content area

2. **Create ConversationSidebar.vue component**
   - Implement sidebar structure with `SidebarHeader`, `SidebarContent`, `SidebarFooter`
   - Add "New Conversation" button in header
   - Create conversation list in content area
   - Add empty state when no conversations exist

3. **Update App.vue to use new layout**
   - Replace current structure with `MainLayout`
   - Ensure proper routing integration

### Phase 3: Store Integration
1. **Connect ConversationSidebar to existing stores**
   - Import and use `useCodingAgent` store
   - Import and use history store (if exists)
   - Implement reactive conversation list

2. **Add conversation management methods**
   - `startNewConversation()` method
   - `selectConversation(id)` method
   - Real-time conversation updates

3. **Handle loading and error states**
   - Add skeleton loading for conversation list
   - Handle empty conversation states
   - Add error handling for store operations

### Phase 4: Polish and Optimization
1. **Implement responsive design**
   - Test sidebar behavior on mobile/tablet
   - Adjust breakpoints and spacing
   - Ensure proper touch targets

2. **Add accessibility features**
   - Keyboard navigation support
   - ARIA labels and roles
   - Focus management
   - Screen reader compatibility

3. **Performance optimization**
   - Implement virtual scrolling for large conversation lists
   - Add proper component lazy loading
   - Optimize re-renders with proper reactive patterns

4. **Theme support**
   - Test light/dark theme switching
   - Ensure proper contrast ratios
   - Validate CSS variable inheritance
