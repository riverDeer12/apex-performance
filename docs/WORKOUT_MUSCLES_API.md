# Workout muscles API

Workouts have the muscles they work (codes): `muscles` (primary, doing most of the work) and
`secondaryMuscles` (helping). Codes: `chest, shoulders, biceps, triceps, forearms, abs, obliques, traps,
upper_back, lats, lower_back, glutes, quads, hamstrings, adductors, calves`.

A new workout created without `muscles` gets them guessed from its name (Croatian and English).

## Full body workouts

A workout type is a full body one when its name contains "full body", "total body", "whole body",
"cijelo tijelo" (any form, e.g. "Vježbe za cijelo tijelo") or "corpo intero". For workouts with such a type,
the guess also knows whole body exercises (burpee, thruster, clean, snatch, kettlebell swing, Turkish get-up,
mountain climber, bear crawl, jumps, rower, sled, carries, sprints, …). When nothing in the name is known, the
big muscles are marked (primary: chest, shoulders, upper back, lats, abs, glutes, quads, hamstrings;
secondary: biceps, triceps, lower back, calves).

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).
Authentication: `Authorization: Bearer <login token>`.

| Method | Path | Body | Response | Notes |
|---|---|---|---|---|
| POST | `/workouts/guess-muscles` | `{ "name": { "translations": { "HR": "Burpee", "EN": "Burpee" } }, "workoutTypes": ["<id>", …] }` | `{ "isFullBody": true, "muscles": ["chest", …], "secondaryMuscles": ["shoulders", …] }` | Staff only. Nothing is saved. |

## Suggested form behaviour (as in the apps)

When the coach selects a full body workout type in the workout form, call `POST /workouts/guess-muscles` with
the current names and the selected types. If `isFullBody`, mark the returned muscles in the form: primary ones
as primary, secondary ones as secondary when the muscle isn't marked yet. Muscles the coach already marked stay
as they are. The coach can still change everything before saving.
