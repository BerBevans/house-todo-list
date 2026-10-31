# 0007. Cloudflare Access identity, validated in the app, with a member allowlist

- Status: accepted
- Date: 2026-10-31
- Supersedes: [ADR 0004](0004-google-sign-in-with-a-member-allowlist.md)

## Context

The app is reachable from the internet, and nobody but the house's members may see or change jobs. There are two owners now and possibly a helper later, all with Google accounts. Email and notifications are non-goals, so a password-reset flow would have no way to reach anyone. Inviting a helper (#14) and, later, assigning jobs to people mean the app must know who each person is, and the member list must live in the app.

ADR 0004 chose Google sign-in in the app and turned down Cloudflare Access on the grounds that the app wouldn't know who the user is. That was wrong: Access sends a signed token with every request, carrying the person's email. With Access already in front of the app (ADR 0006), a second sign-in in the app is redundant.

## Decision

Cloudflare Access handles sign-in. Its policy lets in any Google account; it does not decide membership.

On every request, `HouseTodoList.Web` validates the `Cf-Access-Jwt-Assertion` header with ASP.NET Core's JwtBearer handler, reading the token from that header instead of `Authorization`. It checks:

- the signature, against the keys published at `https://<team>.cloudflareaccess.com/cdn-cgi/access/certs`, cached and refreshed when Cloudflare rotates them
- the issuer, which must be the team domain
- the audience, which must be this Access application's AUD tag, so a token from another Access application in the team is refused
- the expiry
- that an `email` claim is present, so service tokens are refused

The issuer, keys URL and audience come from configuration. The app never reads `Cf-Access-Authenticated-User-Email` or the `CF_Authorization` cookie.

`HouseTodoList.Core` then decides whether the verified email belongs to a member of the house (case-insensitively). Anyone else gets a 403 and sees nothing. The first members are seeded from configuration; adding the helper is adding a member, through an invite page (#14).

The app issues no cookie of its own: Access's session length decides how long people stay signed in, and sign-out goes through `/cdn-cgi/access/logout`. ASP.NET Data Protection keys stay on the data volume, because anti-forgery tokens need them to survive a restart.

Without Cloudflare in front, for local development, unit tests and the black-box regression suite, the app is pointed by configuration at a stub issuer that publishes a test key and signs tokens for any email. The stub is test code and never ships in the production image. There is no "skip authentication" setting in any environment.

## Alternatives considered

- Keep Google sign-in in the app (ADR 0004): a second sign-in behind Access, and a Google OAuth client to set up and protect.
- Access alone, with its policy listing the members' emails: no authentication code in the app, but the member list sits in Cloudflare outside the repo, and #14 and job assignment can't be built in the app.
- Trust the `Cf-Access-Authenticated-User-Email` header: anything that reaches the app's port without going through Access could claim any email.
- A sign-in bypass registered only in Development: a bypass living in production code, and the black-box suite would still need real tokens.
- ASP.NET Core Identity with local passwords: we'd store and protect passwords and need a reset flow without email.

## Consequences

No passwords, no OAuth client and no session cookie of our own, and the one code path that checks identity is the same in production and in tests. The app depends on Cloudflare Access, and a misconfigured audience or issuer locks everyone out, which the first deploy must check. The Access policy must stay "any Google account": narrowing it in Cloudflare would quietly override the app's member list. A member without a Google account means adding another identity provider in Access, not in the app.
