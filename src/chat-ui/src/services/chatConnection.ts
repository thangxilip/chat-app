import * as signalR from '@microsoft/signalr'
import type { ChatMessage, SendMessageRequest, UserPresence } from '@/types/chat'

export interface ChatConnectionHandlers {
  onMessage: (message: ChatMessage) => void
  onSystemMessage: (message: string) => void
  onUserJoined: (user: UserPresence) => void
  onUserLeft: (user: UserPresence) => void
  onActiveUsersChanged: (users: UserPresence[]) => void
  onReconnecting: () => void
  onReconnected: () => void
  onClosed: () => void
}

export class ChatConnection {
  private connection?: signalR.HubConnection

  get connectionId() {
    return this.connection?.connectionId ?? null
  }

  async start(displayName: string, handlers: ChatConnectionHandlers) {
    await this.stop()

    const hubUrl = new URL(import.meta.env.VITE_CHAT_HUB_URL ?? 'http://localhost:5013/hubs/chat')
    hubUrl.searchParams.set('displayName', displayName)

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl.toString())
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    this.connection.on('ReceiveMessage', handlers.onMessage)
    this.connection.on('ReceiveSystemMessage', handlers.onSystemMessage)
    this.connection.on('UserJoined', handlers.onUserJoined)
    this.connection.on('UserLeft', handlers.onUserLeft)
    this.connection.on('ActiveUsersChanged', handlers.onActiveUsersChanged)

    this.connection.onreconnecting(handlers.onReconnecting)
    this.connection.onreconnected(handlers.onReconnected)
    this.connection.onclose(handlers.onClosed)

    await this.connection.start()
  }

  async stop() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = undefined
    }
  }

  async sendMessage(request: SendMessageRequest) {
    await this.invoke('SendMessage', request)
  }

  async joinRoom(room: string) {
    await this.invoke('JoinRoom', { room })
  }

  async leaveRoom(room: string) {
    await this.invoke('LeaveRoom', { room })
  }

  async renameUser(displayName: string) {
    await this.invoke('RenameUser', displayName)
  }

  private async invoke(methodName: string, ...args: unknown[]) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Chat connection is not ready.')
    }

    await this.connection.invoke(methodName, ...args)
  }
}
