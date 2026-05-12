export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

export interface ChatMessage {
  id: string
  senderConnectionId: string
  senderDisplayName: string
  message: string
  room: string
  sentAt: string
  isMine?: boolean
}

export interface UserPresence {
  connectionId: string
  displayName: string
  rooms: string[]
  connectedAt: string
}

export interface JoinRoomRequest {
  room: string
}

export interface SendMessageRequest {
  message: string
  room?: string
}
