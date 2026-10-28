# 0003. Litestream backups to Cloudflare R2

- Status: accepted
- Date: 2026-10-28

## Context

The quality bar asks for a nightly backup at most a day old, stored off the machine, with the last 7 kept, and a restore tried before the first release is relied on. The data is one SQLite file (ADR 0002). The owner already has a Cloudflare account.

## Decision

Run Litestream as a sidecar container in the Compose stack. It replicates the SQLite database continuously to a Cloudflare R2 bucket, which is S3-compatible, takes a snapshot every 24 hours and keeps 7 days. Restoring is `litestream restore` to a new file, then starting the app on it. The restore is rehearsed, and written up as a runbook, before the first release is relied on. The R2 credentials live only in the host's environment file.

## Alternatives considered

- A nightly cron job running `sqlite3 .backup` and `rclone`: simpler to follow, but loses up to a day of changes, and the pruning script is ours to get right.
- Backblaze B2: just as good; R2 was chosen because the account already exists.
- VPS provider snapshots: not off the provider, and they restore the whole machine, not the data.

## Consequences

At most seconds of changes are lost, and any point in the last 7 days can be restored, which covers a mistake noticed late. Litestream is one more thing to understand and keep updated, and its health has to be watched through its logs. If a later feature adds files beside the database (photos are a non-goal), they need their own backup.
