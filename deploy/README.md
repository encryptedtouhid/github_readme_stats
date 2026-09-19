# Deployment (GreenCloud VM · Docker · shared Caddy)

Every push to `main` runs `.github/workflows/deploy.yml`:

1. Runs tests, builds the Docker image and pushes it to `ghcr.io/encryptedtouhid/github_readme_stats` (`latest` + commit SHA).
2. Copies `deploy/docker-compose.yml` to `/opt/apps/github-readme-stats` on the VM over SSH, writes `.env`, then `docker compose pull && up -d`.
3. Fails the job if `/health` does not respond within ~60 s.

## On the VM

- App: container `github-readme-stats` on the external `web` network, port 8080 (not published).
- Reverse proxy / TLS: the shared Caddy in `/opt/webstack` — site block in `deploy/caddy-site.snippet`,
  appended to `/opt/webstack/caddy/Caddyfile`. Reload with
  `docker exec caddy caddy reload --config /etc/caddy/Caddyfile`.
- Redis: existing external instance (`REDIS_URL`).

## GitHub secrets (Settings → Secrets and variables → Actions)

| Secret | Value |
|--------|-------|
| `VM_HOST` | GreenCloud VM public IP |
| `VM_USER` | SSH user (in the `docker` group) |
| `VM_SSH_KEY` | Private key whose public half is in `~/.ssh/authorized_keys` on the VM |
| `VM_SSH_PORT` | Optional, defaults to `22` |
| `GH_STATS_TOKEN` | GitHub PAT used by the app to query the GitHub API |
| `REDIS_URL` | Existing Redis connection string (StackExchange.Redis format) |

## DNS / Cloudflare

`github-readme-stats.tuhidulhossain.com` → **A record → VM IP**, proxied (orange cloud), SSL/TLS **Full (strict)**.

## Manual operations

```bash
ssh greencloud
cd /opt/apps/github-readme-stats
docker compose logs -f app                    # tail logs
docker compose ps                             # status
docker compose pull && docker compose up -d   # redeploy image in .env
```
