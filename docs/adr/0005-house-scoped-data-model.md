# 0005. Every row belongs to a house

- Status: accepted
- Date: 2026-10-28

## Context

There is one house now. The product document names a possible future with several houses, each with its own members and a user belonging to more than one, and asks that it not be made needlessly hard. Adding a house key to every table later means a migration across all of them and an audit of every query for leaks between houses.

## Decision

Model the house from the first release. A `Houses` table holds one row, seeded at startup. Members belong to a house, with a role. Every job, and every later table holding house data (areas, links, sub-tasks), carries a `HouseId`, and every query is scoped to the signed-in member's house. Ids are GUIDs, so rows from different houses never clash.

We do not build anything else for several houses: no house switcher, no creating houses, no per-house settings.

## Alternatives considered

- No house concept until it's needed: simplest now, but the later change touches every table and query, and a missed query leaks data between households.
- A separate database per house: strong isolation, but more backups and migrations to manage for a feature that may never come.

## Consequences

One extra column and one extra condition per query, which the repositories own. A query that forgets the house scope is a bug even while there is one house, so repository tests check it. Several houses later becomes mostly a UI and sign-in change.
