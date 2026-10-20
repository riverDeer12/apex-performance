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

Each exercise has `workoutId`, so tapping it in a training opens that workout (`GET /workouts`) with its
description, video and muscles.
