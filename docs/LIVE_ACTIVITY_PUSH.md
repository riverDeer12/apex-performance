# Live Activity push (training in progress)

The Live Activity of a training in progress (lock screen, Dynamic Island) is shown on the client's iPhone and
on all iPhones of the coach, not only on the iPhone that started it. The backend starts, updates and ends it
with ActivityKit push notifications, sent through Firebase Cloud Messaging (HTTP v1, `apns.live_activity_token`;
FirebaseAdmin 3.5.0 `ApnsConfig.LiveActivityToken`). It uses the same Firebase project, credential and APNs key as
the normal pushes. Starting by push needs iOS 17.2+ (the app requires 17.5).

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).
Authentication: `Authorization: Bearer <login token>`, every role.

## Tokens

The iOS app registers the tokens of the logged user's iPhone. The device is identified by its FCM registration
token, which it registered before with `POST /fcm-tokens`; Firebase needs both tokens for a Live Activity push.

| Kind | Swift | Used for |
|---|---|---|
| `pushToStart` | `Activity<TrainingActivityAttributes>.pushToStartTokenUpdates` | `start` on this device |
| `update` | `activity.pushTokenUpdates` of the activity of one training | `update` and `end` of that activity |

Table `LiveActivityTokens`: `Id`, `DeviceTokenId` (FK to `DeviceTokens`, cascade delete), `Kind`
(`PushToStart` / `Update`), `TrainingId` (only for `Update`, no FK: trainings are soft deleted), `Token`,
`UpdatedAt`. One push-to-start token per device and one update token per device and training: a new token
replaces the previous one. Tokens go away with the device token (invalid FCM token, new FCM token), on logout,
when another user logs in on the device (`POST /fcm-tokens` of the same FCM token),
when Firebase reports them invalid, and the update tokens of a training once its activities were ended.

| Method | Path | Body | Notes |
|---|---|---|---|
| PUT | `/live-activity-tokens` | `{ "deviceToken": "<FCM token>", "kind": "pushToStart" \| "update", "trainingId": "guid" \| null, "token": "<hex>" }` | `200`. `404` when the FCM token isn't the logged user's device or the training isn't visible to the user (clients: their started or completed trainings). `trainingId` required for `update`, null for `pushToStart`. An update token of a training that already ended ends that activity right away. |
| POST | `/live-activity-tokens/remove` | `{ "deviceToken": "<FCM token>", "trainingId": "guid" \| null }` | `200`. All tokens of the device (logout), or only the update token of one training. Only the user's own devices. |

`PUT /trainings/{id}/times` takes an optional `"deviceToken": "<FCM token>"`: the iPhone that starts the
training shows its own (local) activity, so it gets no push to start another.

## Sending

`LiveActivityPushJob` (Hangfire) runs after `PUT /trainings/{id}/times` (times changed), `PUT /trainings/{id}`
(name or completion of a started training changed), `PUT /trainings/{id}/completion` and
`DELETE /trainings/{id}` of a started training. It reads the training as it is then. Which device gets what is
in `LiveActivityPush.Plan` (unit tested):

- Training started now (it wasn't running before, now started, not ended, not completed):
  `start` to the iOS devices of the training's **client** and of the **staff user who started it**, that have a
  push-to-start token and no update token for the training yet. Not to the device that sent the request
  (`deviceToken` above). Starting on the web starts it on all the coach's iPhones.
- Still running (start time or name changed): `update` to every device with an update token for the training.
- Not running any more: `end` to every device with an update token, then those tokens are deleted.
  Ended normally: final state with `endedAt`, dismissed 15 minutes after the end (like the app does locally).
  Times cleared, completed without an end, deleted: dismissed right away.

Firebase errors `UNREGISTERED`, `SENDER_ID_MISMATCH` and `INVALID_ARGUMENT` about a token remove the Live Activity
token used. Results are logged per device.

### Payloads

`attributes` and `content-state` decode into the app's `TrainingActivityAttributes` / `ContentState`.

**Dates in `content-state` are Swift `Date`s with ActivityKit's default `JSONDecoder`: a number, seconds since
2001-01-01 00:00 UTC (`timeIntervalSinceReferenceDate`, = Unix time − 978307200).** `timestamp` and
`dismissal-date` are Unix seconds (APNs). `trainingId` and `key` are the training id in upper case, like Swift's
`UUID.uuidString`, which the app compares them with. `endedAt` is left out while the training runs.

Start on the client's iPhone (FCM HTTP v1 `message`, as FirebaseAdmin sends it):

```json
{
  "token": "<FCM registration token>",
  "apns": {
    "headers": { "apns-priority": "10" },
    "live_activity_token": "<push-to-start token>",
    "payload": {
      "aps": {
        "alert": { "title": "Training in progress", "body": "Legs · Marko Marić" },
        "timestamp": 1794391205,
        "event": "start",
        "content-state": {
          "startedAt": 816084000.0,
          "trainingId": "5B0C3F0E-8A7D-4C1B-9E2F-1A2B3C4D5E6F",
          "trainingName": "Legs"
        },
        "attributes-type": "TrainingActivityAttributes",
        "attributes": {
          "key": "5B0C3F0E-8A7D-4C1B-9E2F-1A2B3C4D5E6F",
          "clientName": "Marko Marić",
          "inProgressText": "Training in progress",
          "endedText": "End",
          "endText": "End training",
          "showsEndButton": false
        }
      }
    }
  }
}
```

- `clientName`: on the client's iPhone the name of the coach (or administrator) who started the training,
  empty when unknown (then only the training name shows); on the staff's iPhones the client's name.
- Texts in the device's language (`DeviceTokens.Language`: hr, en, it, de; Croatian otherwise), the same as the
  app's `training_in_progress`, `training_ended`, `end_training` (`PushTexts.LiveActivity`).
- `showsEndButton`: false for the client, who can't end trainings (the widget hides "End training"); true for
  staff. Activities the app starts itself leave it out (= shown).
- `alert` is required by Apple for `start`; it has no sound.

Update (start time or name changed):

```json
"aps": {
  "timestamp": 1794391300,
  "event": "update",
  "content-state": { "startedAt": 816084000.0, "trainingId": "5B0C…", "trainingName": "Legs & core" }
}
```

End of an ended training (visible 15 more minutes with its duration):

```json
"aps": {
  "timestamp": 1794394805,
  "event": "end",
  "content-state": { "startedAt": 816084000.0, "trainingId": "5B0C…", "trainingName": "Legs", "endedAt": 816087600.0 },
  "dismissal-date": 1794395700
}
```

## iOS app

- `LiveActivityPushService` observes `pushToStartTokenUpdates` and `Activity.activityUpdates` from launch (also the
  background launch iOS does for an activity started by push) and each activity's `pushTokenUpdates`, and registers
  them for the current FCM token (again after the FCM token changed or a login). A draft activity (training not
  saved yet) registers once linked to the saved training.
- Two activities of the same training on one iPhone (a push start that crossed the local start): the newer one is
  ended.
- Logout: `POST /live-activity-tokens/remove` for the device, then the activities end.
- A tap on the activity opens `apexperformance://training/<id>`: the training (for clients in their trainings).

## Testing end to end

1. Client and coach log in on iPhones with the TestFlight test app (iOS 17.2+, Live Activities allowed for the
   app). Open the app once after installing so it registers the tokens.
2. The coach starts a training of that client on the web (or on another iPhone): the client's iPhone shows it on
   the lock screen without the "End training" button, and the coach's iPhones show it with the button.
3. The coach ends it: it shows the duration on every iPhone and goes away after 15 minutes. Clearing the times or
   deleting the training removes it right away.
4. Hangfire dashboard (`/jobs`) and the logs (`Live Activity start of training … sent to device …`) show what was
   sent.

## Limitations

- iOS decides: Live Activities turned off for the app, Low Power Mode or the budget of push updates can drop a
  push. Started activities end after at most 8 hours (12 on the lock screen).
- Moving a running training to another client doesn't move its activity.
- An iPhone that was off when the training started may not get the start (APNs keeps only the last push).
