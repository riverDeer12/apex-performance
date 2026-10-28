# Suggested workouts API

When the coach adds exercises to a training (or a training template), the apps show the workouts that fit the
training's name first.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`). Staff only.

| Method | Path | Response |
|---|---|---|
| GET | `/workouts/suggested?name=Gornji%20dio%20tijela` | `["<workout id>", …]`, best first, at most 12; `[]` for an empty name or nothing that fits. |

How the workouts are picked (`WorkoutSuggestions` in the API):

1. Workouts used in earlier trainings (the last 1000) and in templates whose name shares at least half of its
   words with the given name, e.g. "Ponedjeljak A" → the exercises of earlier "Ponedjeljak A" trainings.
2. Workouts for the muscles or category the name mentions, in Croatian or English: upper/gornji, lower/donji/noge/legs,
   push, pull, prsa/chest, leđa/back, ramena/shoulders, ruke/arms, core/trup/trbuh, gluteus, full body/cijelo tijelo,
   mobilnost/istezanje/stretch (mobility and flexibility category), snaga/kondicija (strength and conditioning).
3. Workouts whose name or type shares a word with the training's name, e.g. "Squat focus" → squats.

Words like "trening", "dan/day" and "tijela/body" are ignored.

## Suggested app behaviour (as in the apps)

In the exercise picker, while the search is empty and the training has a name, show a "Suggested for this training"
section with these workouts above the list of all workouts.
