# 0001. One Razor Pages app on a VPS with Docker Compose

- Status: superseded by [ADR 0006](0006-razor-pages-app-on-a-vps-behind-cloudflare-tunnel.md)
- Date: 2026-10-28

## Context

Two owners, later one helper, use the list mostly from phones, at home and at shops, so it must be reachable from the internet. The quality bar asks for every page and action within 2 seconds over mobile data, automatic restart after a crash, and data that survives a host reboot. High availability and more than one instance are non-goals. Seeing the other owner's changes on refresh is enough; live updates aren't asked for.

## Decision

One process, `HouseTodoList.Web`: an ASP.NET Core app using Razor Pages. Pages are rendered on the server, forms post and redirect, and there is no SPA or persistent connection. It runs on a small cloud VPS in Docker Compose, beside Caddy (the only public entry point, with automatic HTTPS) and Litestream (backups, ADR 0003). Each container has `restart: unless-stopped`, and Docker starts at boot.

## Alternatives considered

- Home server or NAS behind a tunnel: free, but the app's uptime becomes the house's power and broadband, and the internet-facing host is on the home network.
- Blazor Server: needs a live connection per user, which drops on mobile data and shows reconnect banners.
- A SPA with an API: two deployables and a JavaScript build for a list of a few hundred items.
- A managed platform (App Service, Fly.io and similar): fine, but a persistent volume for SQLite is awkward or costly on most of them.

## Consequences

Small pages, no client build, and one image to deploy. The VPS is ours to patch and secure. Live updates, if they're ever wanted, mean adding a small script or SignalR to the pages concerned. Needing more than one instance would mean revisiting this and ADR 0002 together.
