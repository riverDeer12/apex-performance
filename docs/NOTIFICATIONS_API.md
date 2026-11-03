# Notifications list API

Every push notification except chat messages is also kept for its user, so the mobile apps can show a list
(bell icon) of notifications the user missed or dismissed. Chat has its own unread messages.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).
Authentication: `Authorization: Bearer <login token>`, every role.

A notification is kept for users who have a registered device (`POST /fcm-tokens`), in the language of their
app, when the push is sent.

```jsonc
// Notification
{
  "id": "guid",
  "type": "training",                       // push "type": appointment_request, appointment_updated,
                                            // body_measurement, client_goal, monthly_review,
                                            // monthly_review_reminder, training
  "title": "Novi trening",
  "body": "Trener ti je dodao trening: Snaga A",
  "data": { "type": "training", "trainingId": "guid" },  // same as the push data: open the same screen
  "createdAt": "2026-10-20T08:15:02+00:00",
  "readAt": "..." | null
}
```

| Method | Path | Response | Notes |
|---|---|---|---|
| GET | `/notifications` | `Notification[]` newest first | The latest 100. |
| GET | `/notifications/unread-count` | `{ "count": 3 }` | For the badge on the bell. |
| POST | `/notifications/{id}/read` | `204` | When the user opens it from the list. `404` if not theirs. |
| POST | `/notifications/read-all` | `204` | "Mark all as read". |
| DELETE | `/notifications/read` | `204` | "Clear read": deletes the read ones. |
