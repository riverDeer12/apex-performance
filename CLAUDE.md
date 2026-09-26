# apex-performance-api

ASP.NET (.NET 8) backend for the Apex Performance fitness-coaching app.
Production API: https://apex-performance.fit/api — Test API: https://test.apex-performance.fit/api

## Branches

- `production` — the working branch. Make changes here.
- `test` — deploys to the test API via the "Deploy to Test" GitHub Action, no approval needed.
- Pushing to `production` triggers "Deploy to Production", which requires a manual approval on the
  `production` GitHub Environment before it runs (the repo owner approves it in GitHub's UI). This
  deploy also applies EF Core migrations, so treat it as carefully as a real production release.

## Required workflow for any change

Never push straight to `production` without going through `test` first:

1. Make the change on `production` (the working tree).
2. `git stash push -u` the changed files (or just the ones relevant to this change).
3. `git checkout test && git pull origin test --ff-only`
4. `git stash pop`
5. Commit, then `git push origin test`.
6. Wait for the "Deploy to Test" GitHub Action to finish (`gh run list --branch test`, poll with `gh run view <id>`).
7. Verify the change against the test API before going further — hit the real endpoint (or the frontend
   pointed at it) and confirm the change is actually live; do not fabricate verification.
8. `git checkout production && git pull origin production --ff-only && git merge test --no-edit`
9. `git push origin production`. If this is blocked by a permission prompt, stop and ask the user how
   they'd like to proceed rather than retrying it yourself.
10. Tell the user the deploy is waiting on their manual approval of the GitHub Environment gate.
11. Wait for the user to say they approved it, then re-verify with `gh run view <id> --json status,conclusion`
    — do not assume the approval took effect just because the user said so; if the run is still `waiting`,
    say so and wait for them to actually approve it in GitHub.
12. Once the run is `completed`/`success`, verify the live change against the production API.

## Known gotchas

- Migrations run automatically as part of "Deploy to Production" — a schema change merged to `production`
  is not just a code deploy, it will alter the live database once approved. Double-check migrations are
  correct and reversible before pushing.
- appsettings/Firebase key files were purged from git history entirely during the Azure DevOps → GitHub
  migration — never reintroduce real secrets into tracked files; use the deployment environment's own
  secret store.
- `bin/` and `obj/` under `src/ApexPerformance.API/` and `tests/ApexPerformance.Tests/` are build output —
  don't commit them (they should already be untracked/ignored; verify with `git status` before committing).
