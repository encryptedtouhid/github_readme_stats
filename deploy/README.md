# Deployment (GreenCloud VM · Docker)

Every push to `main` runs `.github/workflows/deploy.yml`:

1. Runs tests, builds the Docker image and pushes it to `ghcr.io/<owner>/github_readme_stats` (`latest` + commit SHA).
2. Copies `docker-compose.yml` and `Caddyfile` to the VM over SSH, writes `.env`, then `docker compose pull && up -d`.
3. Fails the job if `/health` does not respond within ~60 s.

## Stack on the VM

| Service | Image | Role |
|---------|-------|------|
| `caddy` | `caddy:2-alpine` | TLS (Let's Encrypt) + reverse proxy on :80/:443 |
| `app`   | `ghcr.io/…/github_readme_stats` | The .NET 9 API on :8080 (internal) |
| `redis` | `redis:7-alpine` | Cache, persisted in a volume |

## One-time VM setup

```bash
scp deploy/setup-vm.sh <user>@<vm-ip>:~ && ssh <user>@<vm-ip> bash setup-vm.sh
```

## GitHub secrets (Settings → Secrets and variables → Actions)

| Secret | Value |
|--------|-------|
| `VM_HOST` | GreenCloud VM public IP |
| `VM_USER` | SSH user (must be in the `docker` group) |
| `VM_SSH_KEY` | Private key (PEM) whose public half is in `~/.ssh/authorized_keys` on the VM |
| `VM_SSH_PORT` | Optional, defaults to `22` |
| `GH_STATS_TOKEN` | GitHub PAT used by the app to query the GitHub API |

## DNS / Cloudflare

`github-readme-stats.tuhidulhossain.com` → **A record → VM IP**, proxied (orange cloud).
Set SSL/TLS mode to **Full (strict)** — Caddy obtains a real certificate, so Cloudflare can verify the origin.

## Manual operations

```bash
ssh <user>@<vm-ip>
cd ~/github-readme-stats
docker compose logs -f app        # tail logs
docker compose ps                 # status
docker compose pull && docker compose up -d   # redeploy latest
```
