<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, useTemplateRef } from 'vue'
import { ChatConnection } from '@/services/chatConnection'
import type { ChatMessage, ConnectionStatus, UserPresence } from '@/types/chat'

interface SystemEvent {
  id: string
  message: string
  createdAt: string
}

const chatConnection = new ChatConnection()
const messages = ref<ChatMessage[]>([])
const users = ref<UserPresence[]>([])
const systemEvents = ref<SystemEvent[]>([])
const displayName = ref(`Guest-${Math.floor(Math.random() * 900 + 100)}`)
const draftDisplayName = ref(displayName.value)
const activeRoom = ref('general')
const roomInput = ref('')
const messageInput = ref('')
const connectionStatus = ref<ConnectionStatus>('disconnected')
const errorMessage = ref('')
const messageList = useTemplateRef<HTMLElement>('messageList')

const connectionLabel = computed(() => {
  switch (connectionStatus.value) {
    case 'connected':
      return 'Connected'
    case 'connecting':
      return 'Connecting'
    case 'reconnecting':
      return 'Reconnecting'
    default:
      return 'Disconnected'
  }
})

const canSendMessage = computed(() => {
  return connectionStatus.value === 'connected' && messageInput.value.trim().length > 0
})

const visibleUsers = computed(() => {
  return users.value.filter((user) => user.rooms.includes(activeRoom.value))
})

const roomNames = computed(() => {
  const names = new Set(['general'])

  users.value.forEach((user) => {
    user.rooms.forEach((room) => names.add(room))
  })

  names.add(activeRoom.value)

  return [...names].sort()
})

async function connect() {
  errorMessage.value = ''
  connectionStatus.value = 'connecting'
  displayName.value = draftDisplayName.value.trim() || displayName.value

  try {
    await chatConnection.start(displayName.value, {
      onMessage: handleMessage,
      onSystemMessage: addSystemEvent,
      onUserJoined: (user) => addSystemEvent(`${user.displayName} joined.`),
      onUserLeft: (user) => addSystemEvent(`${user.displayName} left.`),
      onActiveUsersChanged: (activeUsers) => {
        users.value = activeUsers
      },
      onReconnecting: () => {
        connectionStatus.value = 'reconnecting'
        addSystemEvent('Connection lost. Reconnecting...')
      },
      onReconnected: () => {
        connectionStatus.value = 'connected'
        addSystemEvent('Connection restored.')
      },
      onClosed: () => {
        connectionStatus.value = 'disconnected'
        addSystemEvent('Connection closed.')
      },
    })

    connectionStatus.value = 'connected'
  } catch (error) {
    connectionStatus.value = 'disconnected'
    errorMessage.value = getErrorMessage(error)
  }
}

async function disconnect() {
  await chatConnection.stop()
  connectionStatus.value = 'disconnected'
  users.value = []
  addSystemEvent('Disconnected.')
}

async function sendMessage() {
  const message = messageInput.value.trim()

  if (!message) {
    return
  }

  await runChatAction(async () => {
    await chatConnection.sendMessage({
      message,
      room: activeRoom.value,
    })

    messageInput.value = ''
  })
}

async function joinRoom() {
  const room = normalizeRoom(roomInput.value)

  if (!room) {
    return
  }

  await runChatAction(async () => {
    await chatConnection.joinRoom(room)
    activeRoom.value = room
    roomInput.value = ''
  })
}

async function selectRoom(room: string) {
  activeRoom.value = room

  if (!isCurrentUserInRoom(room)) {
    await runChatAction(() => chatConnection.joinRoom(room))
  }
}

async function leaveCurrentRoom() {
  if (activeRoom.value === 'general') {
    return
  }

  const room = activeRoom.value

  await runChatAction(async () => {
    await chatConnection.leaveRoom(room)
    activeRoom.value = 'general'
  })
}

async function renameUser() {
  const nextName = draftDisplayName.value.trim()

  if (!nextName) {
    return
  }

  await runChatAction(async () => {
    await chatConnection.renameUser(nextName)
    displayName.value = nextName
    addSystemEvent(`Renamed to ${nextName}.`)
  })
}

function handleMessage(message: ChatMessage) {
  messages.value.push({
    ...message,
    isMine: message.senderConnectionId === chatConnection.connectionId,
  })

  scrollMessagesToBottom()
}

function addSystemEvent(message: string) {
  systemEvents.value.unshift({
    id: crypto.randomUUID(),
    message,
    createdAt: new Date().toISOString(),
  })

  systemEvents.value = systemEvents.value.slice(0, 6)
}

async function runChatAction(action: () => Promise<void>) {
  errorMessage.value = ''

  try {
    await action()
  } catch (error) {
    errorMessage.value = getErrorMessage(error)
  }
}

function isCurrentUserInRoom(room: string) {
  const connectionId = chatConnection.connectionId

  return users.value.some((user) => user.connectionId === connectionId && user.rooms.includes(room))
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function normalizeRoom(room: string) {
  return room.trim().toLowerCase().replaceAll(/\s+/g, '-')
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Something went wrong.'
}

async function scrollMessagesToBottom() {
  await nextTick()
  messageList.value?.scrollTo({ top: messageList.value.scrollHeight })
}

onBeforeUnmount(() => {
  void chatConnection.stop()
})
</script>

<template>
  <main class="app-shell">
    <section class="chat-layout" aria-label="Chat workspace">
      <aside class="sidebar" aria-label="Connection and rooms">
        <div class="brand-block">
          <span class="status-dot" :class="connectionStatus" aria-hidden="true"></span>
          <div>
            <p class="eyebrow">SignalR Chat</p>
            <h1>{{ connectionLabel }}</h1>
          </div>
        </div>

        <form class="panel-form" @submit.prevent="connect">
          <label for="displayName">Display name</label>
          <div class="inline-group">
            <input
              id="displayName"
              v-model="draftDisplayName"
              type="text"
              maxlength="32"
              autocomplete="name"
              :disabled="connectionStatus === 'connecting'"
            />
            <button v-if="connectionStatus !== 'connected'" type="submit">Connect</button>
            <button v-else type="button" @click="renameUser">Rename</button>
          </div>
        </form>

        <button
          v-if="connectionStatus === 'connected'"
          class="secondary-button"
          type="button"
          @click="disconnect"
        >
          Disconnect
        </button>

        <form class="panel-form" @submit.prevent="joinRoom">
          <label for="room">Room</label>
          <div class="inline-group">
            <input
              id="room"
              v-model="roomInput"
              type="text"
              placeholder="frontend"
              :disabled="connectionStatus !== 'connected'"
            />
            <button type="submit" :disabled="connectionStatus !== 'connected'">Join</button>
          </div>
        </form>

        <nav class="room-list" aria-label="Rooms">
          <button
            v-for="room in roomNames"
            :key="room"
            type="button"
            :class="{ active: activeRoom === room }"
            @click="selectRoom(room)"
          >
            <span>#{{ room }}</span>
            <small>{{ users.filter((user) => user.rooms.includes(room)).length }}</small>
          </button>
        </nav>
      </aside>

      <section class="chat-panel" aria-label="Messages">
        <header class="chat-header">
          <div>
            <p class="eyebrow">Room</p>
            <h2>#{{ activeRoom }}</h2>
          </div>
          <button
            type="button"
            class="secondary-button compact"
            :disabled="activeRoom === 'general' || connectionStatus !== 'connected'"
            @click="leaveCurrentRoom"
          >
            Leave
          </button>
        </header>

        <p v-if="errorMessage" class="error-message" role="alert">{{ errorMessage }}</p>

        <div ref="messageList" class="message-list">
          <p v-if="messages.length === 0" class="empty-state">No messages yet.</p>

          <article
            v-for="message in messages"
            :key="message.id"
            class="message-row"
            :class="{ mine: message.isMine }"
          >
            <div class="message-bubble">
              <div class="message-meta">
                <strong>{{ message.senderDisplayName }}</strong>
                <span>{{ formatTime(message.sentAt) }}</span>
              </div>
              <p>{{ message.message }}</p>
            </div>
          </article>
        </div>

        <form class="composer" @submit.prevent="sendMessage">
          <div class="composer-inner">
            <input
              v-model="messageInput"
              type="text"
              placeholder="Write a message"
              autocomplete="off"
              :disabled="connectionStatus !== 'connected'"
            />
            <button type="submit" :disabled="!canSendMessage">Send</button>
          </div>
        </form>
      </section>

      <aside class="activity-panel" aria-label="Users and activity">
        <section>
          <div class="section-heading">
            <p class="eyebrow">Online</p>
            <h2>{{ visibleUsers.length }}</h2>
          </div>

          <ul class="user-list">
            <li v-for="user in visibleUsers" :key="user.connectionId">
              <span>{{ user.displayName }}</span>
              <small>{{ user.connectionId.slice(0, 6) }}</small>
            </li>
          </ul>
        </section>

        <section>
          <div class="section-heading">
            <p class="eyebrow">Activity</p>
            <h2>{{ systemEvents.length }}</h2>
          </div>

          <ul class="event-list">
            <li v-for="event in systemEvents" :key="event.id">
              <span>{{ event.message }}</span>
              <small>{{ formatTime(event.createdAt) }}</small>
            </li>
          </ul>
        </section>
      </aside>
    </section>
  </main>
</template>

<style scoped>
:global(*) {
  box-sizing: border-box;
}

:global(body) {
  margin: 0;
  min-width: 320px;
  min-height: 100vh;
  color: #18202b;
  background: #eef2f5;
  font-family:
    Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

:global(button),
:global(input) {
  font: inherit;
}

button {
  min-height: 42px;
  border: 0;
  border-radius: 8px;
  padding: 0 16px;
  color: #fff;
  background: #246bfe;
  cursor: pointer;
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

input {
  width: 100%;
  min-width: 0;
  min-height: 42px;
  border: 1px solid #c9d3df;
  border-radius: 8px;
  padding: 0 12px;
  color: #18202b;
  background: #fff;
}

input:focus {
  border-color: #246bfe;
  outline: 3px solid rgb(36 107 254 / 14%);
}

.app-shell {
  min-height: 100vh;
  padding: 24px;
}

.chat-layout {
  display: grid;
  grid-template-columns: minmax(240px, 300px) minmax(0, 1fr) minmax(220px, 280px);
  gap: 16px;
  max-width: 1440px;
  height: calc(100vh - 48px);
  margin: 0 auto;
}

.sidebar,
.chat-panel,
.activity-panel {
  min-height: 0;
  border: 1px solid #d7e0ea;
  border-radius: 8px;
  background: #fff;
  box-shadow: 0 16px 40px rgb(35 48 68 / 8%);
}

.sidebar,
.activity-panel {
  display: flex;
  flex-direction: column;
  gap: 20px;
  padding: 18px;
  overflow: auto;
}

.brand-block,
.chat-header,
.section-heading,
.message-meta,
.inline-group,
.user-list li,
.event-list li {
  display: flex;
  align-items: center;
}

.brand-block {
  gap: 12px;
}

.status-dot {
  width: 12px;
  height: 12px;
  border-radius: 999px;
  background: #9aa7b5;
}

.status-dot.connected {
  background: #16885f;
}

.status-dot.connecting,
.status-dot.reconnecting {
  background: #d6850b;
}

.eyebrow {
  margin: 0 0 4px;
  color: #657386;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

h1,
h2 {
  margin: 0;
  color: #111827;
  font-size: 1.25rem;
  line-height: 1.2;
}

.panel-form {
  display: grid;
  gap: 8px;
}

.panel-form label {
  color: #3d4a5c;
  font-size: 0.9rem;
  font-weight: 700;
}

.inline-group {
  gap: 8px;
}

.inline-group button {
  flex: 0 0 auto;
}

.secondary-button {
  border: 1px solid #c9d3df;
  color: #233044;
  background: #fff;
}

.secondary-button.compact {
  min-height: 36px;
}

.room-list {
  display: grid;
  gap: 8px;
}

.room-list button {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: #233044;
  background: #f4f7fa;
}

.room-list button.active {
  color: #fff;
  background: #233044;
}

.room-list small,
.user-list small,
.event-list small,
.message-meta span {
  color: #758294;
  font-size: 0.78rem;
}

.room-list button.active small {
  color: #dbe5ef;
}

.chat-panel {
  display: grid;
  grid-template-rows: auto auto minmax(0, 1fr) auto;
}

.chat-header {
  justify-content: space-between;
  gap: 16px;
  border-bottom: 1px solid #e2e8f0;
  padding: 18px 20px;
}

.error-message {
  margin: 16px 20px 0;
  border: 1px solid #f0b9b2;
  border-radius: 8px;
  padding: 10px 12px;
  color: #8f2417;
  background: #fff2f0;
}

.message-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow: auto;
  padding: 20px;
}

.empty-state {
  margin: auto;
  color: #657386;
}

.message-row {
  display: flex;
}

.message-row.mine {
  justify-content: flex-end;
}

.message-bubble {
  width: fit-content;
  max-width: min(620px, 82%);
  border-radius: 8px;
  padding: 10px 12px;
  background: #f1f5f9;
}

.message-row.mine .message-bubble {
  color: #fff;
  background: #246bfe;
}

.message-row.mine .message-meta span {
  color: #dce7ff;
}

.message-meta {
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 4px;
}

.message-bubble p {
  overflow-wrap: anywhere;
  margin: 0;
  line-height: 1.45;
}

.composer {
  border-top: 1px solid #e2e8f0;
  padding: 14px 20px;
}

.composer-inner {
  display: grid;
  grid-template-columns: minmax(280px, 720px) auto;
  justify-content: center;
  gap: 10px;
  width: 100%;
  max-width: 860px;
  margin: 0 auto;
}

.composer-inner input {
  min-height: 40px;
}

.composer-inner button {
  min-width: 88px;
  min-height: 40px;
}

.activity-panel section {
  display: grid;
  gap: 12px;
}

.section-heading {
  justify-content: space-between;
}

.user-list,
.event-list {
  display: grid;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.user-list li,
.event-list li {
  justify-content: space-between;
  gap: 12px;
  border-radius: 8px;
  padding: 10px;
  background: #f4f7fa;
}

.event-list li {
  align-items: flex-start;
  flex-direction: column;
}

@media (max-width: 1080px) {
  .chat-layout {
    grid-template-columns: minmax(220px, 280px) minmax(0, 1fr);
  }

  .activity-panel {
    display: none;
  }
}

@media (max-width: 760px) {
  .app-shell {
    padding: 12px;
  }

  .chat-layout {
    grid-template-columns: 1fr;
    height: auto;
    min-height: calc(100vh - 24px);
  }

  .sidebar,
  .chat-panel {
    min-height: auto;
  }

  .chat-panel {
    min-height: 68vh;
  }

  .composer {
    padding: 12px;
  }

  .composer-inner {
    grid-template-columns: 1fr;
    max-width: none;
  }
}
</style>
