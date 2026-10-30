# Training start, end and heart rate API

The coach marks when a training really starts and ends. The client's iPhone then reads the client's heart rate in
between from Apple Health (measured by an Apple Watch or a chest strap) and sends the lowest, highest and average
value. Only this summary is kept on the server, not the measurements.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).

Trainings (`GET /trainings`, `POST`, `PUT /trainings/{id}`) now also have:

```jsonc
{
  "startedAt": "2026-10-30T08:00:12+00:00" | null,   // marked with "Start"
  "endedAt": "2026-10-30T09:02:40+00:00" | null,     // marked with "End"
  "heartRate": { "min": 72, "max": 168, "average": 124 } | null   // beats per minute
}
```

| Method | Path | Body | Response | Notes |
|---|---|---|---|---|
| PUT | `/trainings/{id}/times` | `{ "startedAt": "…" \| null, "endedAt": "…" \| null }` | `{ id, startedAt, endedAt, heartRate }` | Staff. "Start" sends `startedAt` = now and `endedAt` = null, "End" keeps `startedAt` and sends `endedAt` = now. Both null clears them. `endedAt` must be after `startedAt` and at most 6 h later (else `400`, code `1001`). Changed times clear the heart rate. |
| PUT | `/trainings/{id}/heart-rate` | `{ "min": 72, "max": 168, "average": 124 }` | `{ min, max, average }` | **Client only**, own completed training with both times (else `400`, code `1001`). Values 25–250, `min ≤ average ≤ max`. |

## Suggested app behaviour (as in the apps)

- Staff, in the training details: "Start training" while not started, then "End training", then the start and end
  time with the duration (and a way to clear them).
- Client's iPhone: after the client connects Apple Health in a completed training, the app reads the heart rate
  between `startedAt` and `endedAt` and sends it; later trainings are sent automatically when the client opens
  their trainings. Everyone sees the lowest, highest and average heart rate in the training details.
