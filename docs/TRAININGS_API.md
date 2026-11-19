# Trainings API: drop sets

Sets of a training exercise (in `GET /trainings`, `POST /trainings`, `PUT /trainings/{id}` and the same for
training templates) have `isDropSet`:

```json
"sets": [
  { "reps": "10", "weight": 80, "isDropSet": false },
  { "reps": "8", "weight": 60, "isDropSet": true },
  { "reps": "6", "weight": 40, "isDropSet": true }
]
```

A drop set is done right after the previous set without rest: the same exercise with other reps and less
weight. The sets above are one set of 10 × 80 kg with two drops. `isDropSet` is optional in requests
(`false` by default) and ignored on the first set of an exercise. Apps show drops indented under their set and
don't count them as separate sets.

## Exercise in a training

Each exercise has `workoutId`, so tapping it in a training opens that workout with its description, video and
muscles: `GET /workouts/{id}` returns one workout (same as an item of `GET /workouts`). Clients get the workouts
their plan includes and also the ones in their own trainings; others give `404`.

## Duration of a set

Every set can have `durationSeconds` (optional, 1–86400): how long the set lasted, for any exercise. The apps
show a duration input (min:s) next to repetitions and weight on every set, none of them required, and show the
duration in the training (e.g. `8 × 60 kg · 0:45`, `1:30`, or `1:30 · 10 kg`). Whether a workout is "timed"
no longer matters: `isTimed` is still returned and accepted for older apps, the current apps ignore it.

```json
"sets": [
  { "reps": null, "weight": null, "isDropSet": false, "durationSeconds": 60 },
  { "reps": null, "weight": 10, "isDropSet": false, "durationSeconds": 45 }
]
```

## Personal records of held sets

`GET /personal-records` also returns records of type `LongestDuration` for sets held for a time without
repetitions (e.g. a plank): `value` is the longest such set in seconds (show it as min:s), `weight` the weight of
that set (0 without weight). These held sets don't count for `MaxWeight` and `EstimatedOneRepMax`; sets with
repetitions count for them whether or not they have a duration.

## Sessions: clients training at the same time

The apps' session form makes one training per client, all with one time. They share `sessionId` (a Guid the app
generates once per session):

- `POST /trainings` and `PUT /trainings/{id}` accept an optional `sessionId`. On `PUT`, a missing or null
  `sessionId` keeps the training's session (older apps), `"00000000-0000-0000-0000-000000000000"` takes the
  training out of its session.
- Training responses have `sessionId` (null for a training on its own).
- `GET /trainings/{id}/session`: the trainings of its session that the user may see, the training itself
  included, oldest first. Trainings saved before `sessionId` are grouped by the same creator and the same start to
  the second (`TrainingSessions.SameSession`, unit tested). A training on its own returns only itself; `404` when
  the training isn't visible.
- The end reminder of a session (docs/LIVE_ACTIVITY_PUSH.md) groups by `sessionId` when there is one.
