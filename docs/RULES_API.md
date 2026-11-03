# Cooperation rules API

Clients with individual trainings (plan `PrivateCoaching`) accept the "Pravila suradnje za individualne
treninge" in the app. Each acceptance is kept as a record: client, user, version, time, IP address and
device (user agent). A new version of the rules has to be accepted again.

Until the rules are published (`"Rules": { "Published": true }` in the server settings, after the legal review),
`isRequired` is `false` for everyone on production, so the apps don't ask for them. On test they are asked for.

Base URL: `https://apex-performance.fit/api` (test: `https://test.apex-performance.fit/api`).
Authentication: `Authorization: Bearer <login token>`.

```jsonc
// Rules (GET /rules, POST /rules/accept)
{
  "document": "individual-coaching",
  "version": "1.1",                          // version to accept, send it back in POST /rules/accept
  "documentUrl": "https://apex-performance.fit/assets/documents/pravila-individualni-treninzi-v1.1.pdf",
  "isRequired": true,                        // false for OnlineCoaching / Membership clients
  "isAccepted": false,                       // the current version is accepted
  "acceptedVersion": "1.0" | null,           // last accepted version
  "acceptedAt": "2026-10-22T08:15:02+00:00" | null
}
```

| Method | Path | Body | Response | Notes |
|---|---|---|---|---|
| GET | `/rules` | – | `Rules` | **Client only.** |
| POST | `/rules/accept` | `{ "version": "1.1", "guardianName": "Ana Kovač" \| null, "independentTravelApproved": false }` | `Rules` | **Client only.** `version` must be the current one (else `400`, code `1001`: reload and show the new version). Accepting the same version again keeps the first record. |
| GET | `/rules/acceptances` | – | `{ clientId, clientName, plan, isRequired, version, isCurrent, acceptedAt, guardianName, independentTravelApproved }[]` | Administrators: all clients; coaches: their clients. |

## Suggested app flow

1. After login, a client calls `GET /rules`. When `isRequired && !isAccepted`, show a screen the client can't
   skip (logging out is the only other way out) with:
   - a link that opens `documentUrl` (the PDF),
   - a checkbox with exactly: „Pročitao/la sam i prihvaćam Pravila suradnje za individualne treninge, verzija {version}.”
     — unchecked by default, the accept button is enabled only when it is checked,
   - an optional "accepting as a parent/guardian of a minor client" section: the parent's full name
     (`guardianName`) and a separate, optional checkbox approving coming and leaving alone between 14 and 18
     (`independentTravelApproved`, sent only together with `guardianName`).
2. `POST /rules/accept` and continue into the app.
3. Keep the rules reachable later (e.g. in the profile) with the accepted version and date.
