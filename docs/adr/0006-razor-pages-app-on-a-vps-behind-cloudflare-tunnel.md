# 0006. One Razor Pages app on a VPS behind Cloudflare Tunnel

- Status: accepted
- Date: 2026-10-31
- Supersedes: [ADR 0001](0001-single-razor-pages-app-on-a-vps.md)

## Context

Two owners, later one helper, use the list mostly from phones, at home and at shops, so it must be reachable from the internet. The quality bar asks for every page and action within 2 seconds over mobile data, automatic restart after a crash, and data that survives a host reboot. High availability and more than one instance are non-goals. Seeing the other owner's changes on refresh is enough; live updates aren't asked for.

ADR 0001 put Caddy on the VPS as the public entry point. The harness decisions doc puts the app behind Cloudflare Tunnel and Cloudflare Access instead, with no inbound ports, and Access now carries identity too (ADR 0007). This ADR restates ADR 0001's app decision and replaces its entry point.

## Decision

One process, `HouseTodoList.Web`: an ASP.NET Core app using Razor Pages. Pages are rendered on the server, forms post and redirect, and there is no SPA or persistent connection.

It runs on a small cloud VPS in Docker Compose, beside `cloudflared` (Cloudflare Tunnel) and Litestream (backups, ADR 0003). `cloudflared` connects outbound to Cloudflare and forwards requests to the app over the Compose network. The app's port is never published on the host, and the VPS firewall drops all inbound traffic. Cloudflare terminates HTTPS. Each container has `restart: unless-stopped`, and Docker starts at boot.

The owner reaches the VPS by SSH through the same tunnel: `cloudflared` routes an SSH hostname to `localhost:22`, behind an Access policy that allows only the owner's email, and the owner's SSH client uses `cloudflared access ssh` as its proxy. SSH accepts keys only. The VPS provider's web console is the way in if the tunnel itself is broken.

## Alternatives considered

- Keep Caddy (ADR 0001): needs ports 80 and 443 open on the VPS, and leaves identity to the app.
- Home server or NAS behind a tunnel: free, but the app's uptime becomes the house's power and broadband, and the internet-facing host is on the home network.
- Blazor Server: needs a live connection per user, which drops on mobile data and shows reconnect banners.
- A SPA with an API: two deployables and a JavaScript build for a list of a few hundred items.
- A managed platform (App Service, Fly.io and similar): fine, but a persistent volume for SQLite is awkward or costly on most of them.
- SSH through Tailscale, or port 22 open to the owner's IP: another account and agent on the box, or an inbound port that breaks whenever the home IP changes.

## Consequences

Small pages, no client build, one image to deploy, and nothing on the VPS that the internet can reach directly. The app depends on Cloudflare being up, and on the tunnel token, which lives only in the host's environment file. The VPS is still ours to patch. Live updates, if they're ever wanted, mean adding a small script or SignalR to the pages concerned. Needing more than one instance would mean revisiting this and ADR 0002 together.
