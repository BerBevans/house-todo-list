# Deploying

A push to `main` runs `.github/workflows/deploy.yml`. It builds the image, pushes it to
`ghcr.io/berbevans/house-todo-list`, then SSHs to the server through Cloudflare Access as the `deploy`
user, sending only the image's digest. The server pulls that digest, restarts the app and checks
`/healthz` on loopback for up to 60 seconds. If the new version isn't healthy it puts the previous one
back and the workflow fails. Only after a healthy deploy does the workflow move the `live` tag.

The files here are the reference copy of the server setup. They're installed by hand and never by CI,
and they're guardrails: changing one needs the owner's review, then installing again.

| File | Installed at | Owner, mode |
| --- | --- | --- |
| `compose.yaml` | `/opt/house-todo/compose.yaml` | root, 0644 |
| `house-todo-deploy` | `/usr/local/sbin/house-todo-deploy` | root, 0755 |
| `deploy-request` | `/usr/local/bin/house-todo-deploy-request` | root, 0755 |
| `sudoers` | `/etc/sudoers.d/house-todo-deploy` | root, 0440 |
| `authorized_keys.example` | `/home/deploy/.ssh/authorized_keys`, with the real key | deploy, 0600 |

## Setting up

### On the server

1. Let the deploy user in over SSH: in `/etc/ssh/sshd_config.d/00-hardening.conf`, change
   `AllowUsers ben` to `AllowUsers ben deploy`, then `sudo sshd -t && sudo systemctl reload ssh`.
2. Create the user, with no password and not in the `docker` group:
   `sudo adduser --disabled-password --gecos "" --shell /bin/sh deploy`.
3. Copy this folder over (`scp -r deploy house-todo:/tmp/`), then from `/tmp/deploy`:

   ```sh
   sudo install -m 0755 house-todo-deploy /usr/local/sbin/house-todo-deploy
   sudo install -m 0755 deploy-request /usr/local/bin/house-todo-deploy-request
   sudo visudo -cf sudoers && sudo install -m 0440 sudoers /etc/sudoers.d/house-todo-deploy
   sudo install -d -m 0755 /opt/house-todo && sudo install -m 0644 compose.yaml /opt/house-todo/
   sudo install -d -m 0700 -o deploy -g deploy /home/deploy/.ssh
   ```

4. Make the deploy key, in WSL, with no passphrase. The private half goes only to GitHub:

   ```sh
   ssh-keygen -t ed25519 -N "" -C house-todo-deploy -f ~/.ssh/house_todo_deploy
   gh secret set DEPLOY_SSH_KEY --env production -R BerBevans/house-todo-list < ~/.ssh/house_todo_deploy
   shred -u ~/.ssh/house_todo_deploy
   ```

   On the server, write `/home/deploy/.ssh/authorized_keys` as in `authorized_keys.example`, with the
   contents of `~/.ssh/house_todo_deploy.pub` after the options. Owner `deploy`, mode 0600.

### In Cloudflare Zero Trust

5. Access > Service credentials > Service Tokens: create `house-todo-deploy`. Note the client ID and
   secret; the secret is shown once.
6. On the `house-todo SSH` application, add a policy: action Service Auth, include Service Token
   `house-todo-deploy`. Leave the existing email policy as it is.

### In GitHub

7. Create the deploy App, `berbevans-factory-deploy`: no webhook, Contents read and write, installed
   on this repo only. Generate a private key.
8. Settings > Environments > `production`: deployment branches limited to `main`.
   - Secrets: `DEPLOY_SSH_KEY` (step 4), `ACCESS_SERVICE_TOKEN_ID` and `ACCESS_SERVICE_TOKEN_SECRET`
     (step 5), `DEPLOY_APP_PRIVATE_KEY` (step 7).
   - Variables: `DEPLOY_SSH_HOST` (the SSH hostname), `DEPLOY_SSH_KNOWN_HOSTS` (the output of
     `ssh-keygen -F <SSH hostname>` in WSL, without the comment line), `DEPLOY_APP_CLIENT_ID` (step 7).
9. A tag ruleset, `live-tag`, on tags matching `live`: restrict creations, updates and deletions, block
   force pushes, with the deploy App as the only bypass actor.
10. After the first deploy run pushes the image, make the `house-todo-list` package public (Packages >
    `house-todo-list` > Package settings), then rerun the failed deploy job. The server pulls without
    credentials.

## Checking it

- Deploy a good commit: the workflow passes, the app answers, and `live` points at the commit.
- Deploy a broken image (one that never answers `/healthz`): the workflow fails, the previous version
  is running again, and `live` hasn't moved.
- From WSL, `ssh -i <deploy key> deploy@<SSH hostname> ls` is refused: only a digest is accepted.
