# Notifications list API

Every push notification except chat messages and reminders (appointment reminder, training end reminder) is
also kept for its user, so the mobile apps can show a list
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

## Appointment reminder

Two hours before an approved appointment starts, each of its clients gets a push on all their devices (iOS and
Android), in each device's language (`PushTexts.AppointmentReminder`), the time in Croatian time
(Europe/Zagreb):

| Language | Title | Body |
|---|---|---|
| hr | Trening za 2 h | Danas u 18:00 · Ivan Horvat |
| en | Training in 2 h | Today at 18:00 · Ivan Horvat |
| it | Allenamento tra 2 h | Oggi alle 18:00 · Ivan Horvat |
| de | Training in 2 Std. | Heute um 18:00 · Ivan Horvat |

"Sutra u" / "Tomorrow at" / "Domani alle" / "Morgen um" when the appointment starts after midnight; several
coaches are separated by ", ", no coach leaves out " · …".

- `AppointmentReminderJob` (Hangfire, scheduled for start − 2 h) by `AppointmentReminderJob.Schedule`, called
  when an appointment is created approved (`POST /appointments` by a coach, `GET
  /recurring-appointments/generate-next-week`), approved (`GET /appointments/approve/{id}`) or gets new clients
  (`POST /appointments` on an existing one, join requests approved). Once per appointment: the job id and start
  are kept in a Hangfire hash (`appointment-reminder:{id}`), so the same start schedules nothing again and a new
  start replaces the earlier job. At startup, approved appointments ahead without a reminder get one (appointments
  approved before reminders existed).
- Not scheduled when less than 2 hours are left when the appointment is created or approved, even when the start
  is still more than 15 minutes away: the client has just booked it or been told it was approved.
- When it runs, the job reads the appointment and sends only if it is still approved, not deleted, with the same
  start, and at least 15 minutes away (a job that runs late because the server was down). Cancelled, declined or
  deleted appointments need nothing else. The clients are read then: clients who joined get it, those who left
  don't.
- Not kept in the app's notification list.
- Data: `type` = `appointment_reminder`, `appointmentId`. A tap opens that appointment.
- A normal notification on iOS and Android (like the other pushes, which the Android app shows itself in the
  foreground). It expires at the start (`apns-expiration`, Android `ttl`); `apns-collapse-id` and the Android
  notification tag are the appointment id.

```json
{
  "token": "<FCM token of an iPhone>",
  "notification": { "title": "Trening za 2 h", "body": "Danas u 18:00 · Ivan Horvat" },
  "data": { "type": "appointment_reminder", "appointmentId": "7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607" },
  "apns": {
    "headers": { "apns-collapse-id": "7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607", "apns-expiration": "1795194000" },
    "payload": { "aps": { "sound": "default", "thread-id": "7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607" } }
  }
}
```

```json
{
  "token": "<FCM token of an Android phone>",
  "notification": { "title": "Training in 2 h", "body": "Today at 18:00 · Ivan Horvat" },
  "data": { "type": "appointment_reminder", "appointmentId": "7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607" },
  "android": { "priority": "high", "ttl": "7200s", "notification": { "tag": "7d3f1c2a-4b5e-4f60-8a91-b2c3d4e5f607" } }
}
```
