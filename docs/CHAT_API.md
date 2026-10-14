# Chat API (coach ↔ client)

Everything a client application (web, iOS, Android/Flutter) needs to implement the chat between a
coach and one of their clients. The web application (`apex-performance-web`, `src/app/features/chat`)
is the reference implementation.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).

## Rules

- A chat exists only between a coach and one of **their** clients (coach–client link).
- The chat is **open by default**: a client can start a chat with any of their coaches at any time, and a
  coach with any of their clients.
- Only the **coach** can disable (and re-enable) a chat, at any time. A disabled conversation keeps its
  messages and stays visible to both sides as **read only**; nobody can write until the coach enables it
  again. A client can't re-enable it.
- Only the two participants can read a conversation — administrators can't.
- Text messages only (max 4000 characters). Message content is stored **encrypted** in the database
  (AES-256-GCM); the API returns plain text to the participants over HTTPS.
- Roles: endpoints accept users with the `Coach` or `Client` role (the coach-only endpoint is marked).

## Authentication

Same as the rest of the API: `Authorization: Bearer <login token>`.
The real-time hub takes the token in the query string (see below). A token of an ended session
(login on another device, revoked by the administrator) gets `401` — log the user out.

## Models

```jsonc
// ChatParticipant
{ "id": "guid (coach id or client id)", "userId": "guid", "fullName": "Ana Kovač" }
```

ChatContact (someone the logged user can chat with):

```jsonc
{
  "id": "guid (client id for a coach, coach id for a client)",
  "userId": "guid",
  "fullName": "Ana Kovač",
  "conversationId": "guid" | null,  // null: no chat started yet
  "isEnabled": true                 // false: the coach disabled the chat (read only)
}

// ChatMessage
{
  "id": "guid",
  "conversationId": "guid",
  "senderUserId": "guid",          // compare with the logged user's id ("sub" claim) to tell own messages
  "content": "text" | null,         // null when the message can't be decrypted (show a placeholder)
  "createdAt": "2026-10-13T08:15:02.123+00:00",
  "readAt": "..." | null            // when the OTHER participant read it (read receipt for own messages)
}

// ChatConversation
{
  "id": "guid",
  "coach": ChatParticipant,
  "client": ChatParticipant,
  "isEnabled": true,
  "lastMessageAt": "..." | null,
  "lastMessage": ChatMessage | null,
  "unreadCount": 3                  // messages of the other participant the logged user hasn't read
}
```

The other participant is `client` for a coach and `coach` for a client.

## REST endpoints

| Method | Path | Body / query | Response | Notes |
|---|---|---|---|---|
| GET | `/chat/conversations` | – | `ChatConversation[]` (newest activity first) | All own conversations, disabled ones included (read only). |
| GET | `/chat/contacts` | – | `ChatContact[]` (by name) | Coach: their clients. Client: their coaches. For the "new chat" picker. |
| POST | `/chat/conversations/start` | `{ "coachId": "guid" }` | `ChatConversation` | **Client only.** Opens the chat with one of their coaches: creates it (enabled) if needed, otherwise returns the existing one — also when disabled (`isEnabled: false`, read only). `404` if not their coach. |
| POST | `/chat/conversations` | `{ "clientId": "guid", "isEnabled": true }` | `ChatConversation` | **Coach only.** Creates the conversation if needed and enables/disables it. `404` if the client isn't the coach's client. |
| GET | `/chat/conversations/{id}/messages` | `?before=<ISO date>&take=50` (both optional, take ≤ 200) | `ChatMessage[]` oldest → newest | Without `before`: newest messages. For older ones pass `createdAt` of the oldest loaded message. |
| POST | `/chat/conversations/{id}/messages` | `{ "content": "text" }` | `ChatMessage` | `400` with error code `1101` if the coach disabled the conversation (reload it to show it read only), `404` if not visible. Rate limit: 120/min per IP (`429`, error code `1500`). |
| POST | `/chat/conversations/{id}/read` | – | `204` | Marks all messages of the other participant as read. Call when the conversation is open and visible, and when a new message arrives into the open conversation. |
| POST | `/chat/conversations/{id}/report` | `{ "reason": "text" }` (optional, ≤ 1000) | `204` | Either participant reports the conversation (App Store rule for user content). The administrator gets an email with the reason and the last 50 messages — tell the user the messages are shared. Rate limit: 10/hour per IP. |
| GET | `/chat/unread-count` | – | `{ "count": 5 }` | Total unread, for a badge. |

Errors use the usual API shape: `{ "statusCode": 404, "message": "...", "errors": { "generalErrors": ["1200"] } }`.

## Real-time hub (SignalR)

- URL: `https://apex-performance.fit/api/hubs/chat`
- Auth: access token as query parameter `access_token` (the SignalR clients do it with
  `accessTokenFactory` / `accessTokenProvider`).
- Transports: WebSockets, falling back to Server-Sent Events / long polling automatically.
- The hub has **no methods to call**; clients only listen. Send messages with the REST endpoint.
- Events go to all connections of a user (every device/tab):

| Event | Argument | When |
|---|---|---|
| `MessageReceived` | `ChatMessage` | A message was sent in one of the user's conversations — also echoed to the sender's other devices. Deduplicate by `id` (the sender already has it from the POST response). |
| `MessagesRead` | `{ "conversationId": "guid", "readerUserId": "guid", "readAt": "..." }` | The other participant read messages — set `readAt` on own messages up to `readAt`. If `readerUserId` is the logged user, another device of theirs read them: clear the unread badge. |
| `ConversationUpdated` | `"conversation id"` (string) | A chat was started, or the coach disabled/enabled it. Reload `GET /chat/conversations`. |

Reconnect automatically (`withAutomaticReconnect`) and after reconnecting reload the conversation list
and the open conversation's newest messages, as events missed while offline are not replayed.

Flutter: package `signalr_netcore`; iOS native: `SignalRClient` (Swift); Android native:
`com.microsoft.signalr`.

## Push notifications (FCM)

When a message is sent, the recipient's devices get a push (in the language of the app on the device):

- title / body: "Nova poruka" / "Nova poruka od: Ana Kovač" (en: "New message" / "New message from Ana Kovač",
  it: "Nuovo messaggio" / "Nuovo messaggio da: Ana Kovač"). The message text is intentionally **not** included.
- data: `{ "type": "chat_message", "conversationId": "guid" }` — open that conversation on tap.
- Don't show the push (or show it silently) while that conversation is open on screen.

## Suggested app flow

1. After login, connect to the hub and load `GET /chat/unread-count` for the badge.
2. Chat list screen: `GET /chat/conversations`, plus a "new chat" action: pick a person from
   `GET /chat/contacts` (if `conversationId` is set, just open that conversation). Then a coach calls
   `POST /chat/conversations { clientId, isEnabled: true }`, a client `POST /chat/conversations/start { coachId }`.
3. Conversation screen: `GET .../messages`, scroll up loads older (`before`), send with `POST .../messages`,
   call `POST .../read` when opened and on every incoming message while open.
4. On `MessageReceived` for another conversation: update its last message and unread count.
5. Coach can disable/enable the chat from the conversation screen (same POST with `isEnabled`).
   A disabled conversation shows its history with a notice instead of the message input.
