# 0004. Google sign-in with a member allowlist

- Status: accepted
- Date: 2026-10-28

## Context

The app is reachable from the internet, and nobody but the house's members may see or change jobs. There are two owners now and possibly a helper later, all with Google accounts. Email and notifications are non-goals, so a password-reset flow would have no way to reach anyone.

## Decision

Sign in with Google through ASP.NET Core's Google handler, configured in `HouseTodoList.Web`, then issue an app cookie. Every page requires that cookie. After sign-in, `HouseTodoList.Core` decides whether the person is allowed in by matching their verified email against the house's members (case-insensitively); anyone else gets a 403 and sees nothing. The first members are seeded from configuration; adding the helper later is adding a member, first by configuration and later through an invite page. ASP.NET Data Protection keys are kept on the data volume, so a restart doesn't sign everyone out.

## Alternatives considered

- ASP.NET Core Identity with local passwords: we'd store and protect passwords and need a reset flow without email.
- Cloudflare Access in front of the app: keeps auth out of our code, but the app would still need to know who the user is for assigning jobs later, and access rules would sit outside the repo.
- A shared password or a magic link: too weak, or needs email.

## Consequences

No passwords stored and Google's two-factor sign-in for free. It needs a Google Cloud OAuth client, a one-off setup by the owner, and depends on Google being reachable. A member without a Google account would mean adding another external provider beside this one, which the handler model allows.
