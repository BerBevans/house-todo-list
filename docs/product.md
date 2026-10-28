# House To-Do List

## Purpose

We've bought an older house that needs a lot of work, and the jobs are hard to keep track of in our heads or on scraps of paper. This product gives the household one shared list of house jobs that both of us can add to and tick off. Later it grows into a slimmed-down Jira for the house: jobs grouped by room, broken into sub-tasks, prioritised and linked.

## Users

- **Owners (two people, a married couple).** Both add, edit and complete jobs, with equal rights. They use it mostly on phones, around the house and out at shops, and sometimes on a laptop. It's a website that works well in a phone browser, not an app-store app.
- **A trusted helper (later, possibly).** A family member who does jobs around the house and garden. Sees all jobs and may be assigned some. Nothing is hidden from him.
- **Everyone else** must not be able to see, add, change or remove jobs.

## Scope

### First release

- A user can sign in. Someone who isn't one of the two owners can't see, add, change or remove jobs.
- A user can add a job to the house list with a title and an optional free-text description.
- A user can see the whole list, with open jobs first and done jobs below them.
- A user can edit a job's title and description.
- A user can mark a job done, and undo that.
- A user can delete a job.
- A job added or changed by one owner shows for the other when they next load or refresh the list.

### Later

- A user can define the areas of the house (rooms, garden) and assign a job to one; jobs with no area belong to the house as a whole.
- A user can break a job into sub-tasks, in tiers like epic → task → sub-task (for example "Remodel bathroom 2" → "Remove bath", "Buy new bath", "Paint walls").
- A user can give a job a status beyond open/done (for example to do, in progress, blocked, done).
- A user can prioritise jobs and see the list in priority order.
- A user can link related jobs (for example "Buy new bath" blocks "Fit new bath").
- A user can assign a job to a person.
- An owner can invite the helper, who can see jobs and be assigned them.

## Non-goals

- Hiding jobs or activity between users. Everyone who can sign in sees everything.
- More than one house, for now. A possible future direction: several houses, each with its own members, and a user belonging to more than one house (for example the helper adding his own house and sharing it with his wife). Not planned, but not to be made needlessly hard.
- A native mobile app. A phone-friendly website is enough.
- High availability or running more than one instance. Short outages while a crashed container restarts are fine.
- Costs and budgets.
- Shopping lists. "Buy new bath" is an ordinary job.
- Calendar sync.
- Reminders and notifications.
- Photos and attachments. They'd make storage and backups much bigger.
- Time tracking.

## Quality bar

- Every change a user has saved survives an app restart, a container crash and a host reboot.
- If the app crashes, it restarts by itself without anyone stepping in.
- A backup runs every night. At most one day of changes is lost if the stored data is destroyed.
- Backups are stored somewhere other than the machine the app runs on, so they survive a dead disk or host.
- The last 7 nightly backups are kept, so a mistake noticed a few days late can still be undone.
- Restoring from a backup has been tried successfully before the first release is relied on.
- Without signing in, a person can't see, add, change or delete a job. This holds even though the app is reachable from the internet.
- Every page loads and every action completes within 2 seconds on a phone over mobile data, with a few hundred jobs in the list. Speed is otherwise not a concern.
- Every page is usable on a phone screen without zooming or scrolling sideways.
- Text is readable without zooming, buttons are big enough to tap easily, and every page still works when the phone's text-size setting is turned up. Accessibility beyond that, such as screen-reader testing, is not a concern yet.

## Questions for the architecture session

- Where does it run (home server, NAS, a cloud host)? It must be reachable from outside the home network, for example from a DIY shop.
- How do users sign in, and how is a new user (the helper) added later?
- What stores the data, and how is the nightly backup taken, stored off-machine and restored?
- Do the "Later" features (tiers, links, statuses) need a data model that's planned now, so the first release doesn't need rework?
- How cheaply can the design leave room for several houses with their own members, without building it now?
