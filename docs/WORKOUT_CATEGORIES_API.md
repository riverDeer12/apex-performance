# Workout categories API

Every workout has a `category`, for filtering the workout list:

| Value | Name (HR / EN / IT) |
|---|---|
| `MobilityFlexibility` | Mobilnost i fleksibilnost / Mobility and flexibility / Mobilità e flessibilità |
| `StrengthConditioning` | Snaga i kondicija / Strength and conditioning / Forza e condizionamento |

Existing workouts were categorized by their types: a workout with a type whose name contains "mobil",
"stretch", "istez", "fleksib", "flexib" or "allung" is `MobilityFlexibility`, all others `StrengthConditioning`.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).

| Method | Path | Change |
|---|---|---|
| GET | `/workouts`, `/workouts/{id}` | Response has `"category": "MobilityFlexibility" \| "StrengthConditioning"`. |
| POST | `/workouts` | Optional `"category"`. Without it, the category comes from the workout types as above. |
| PUT | `/workouts/{id}` | Optional `"category"`. Without it (null), the category stays. |
| POST | `/workouts/import` | Imported workouts get the category from their types. |

An unknown category returns `400` with code `1001` (NotValid).

## Suggested app behaviour (as in the apps)

- The workout list has two filter buttons, "Mobilnost i fleksibilnost" and "Snaga i kondicija". Tapping one shows
  only that category, tapping it again shows all workouts. Filtering is done on the client.
- The workout form has a category picker; a new workout starts with "Snaga i kondicija" and switches to
  "Mobilnost i fleksibilnost" when a mobility or stretching type is picked (the coach can still change it).
