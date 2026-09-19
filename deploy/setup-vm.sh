#!/usr/bin/env bash
# One-time bootstrap for the GreenCloud VM (Ubuntu/Debian).
# Run as a sudo-capable user:  bash setup-vm.sh
set -euo pipefail

if ! command -v docker >/dev/null 2>&1; then
  echo ">> Installing Docker Engine + Compose plugin"
  curl -fsSL https://get.docker.com | sudo sh
fi

sudo usermod -aG docker "$USER"
sudo systemctl enable --now docker

mkdir -p "$HOME/github-readme-stats"

if command -v ufw >/dev/null 2>&1; then
  echo ">> Opening ports 22, 80, 443"
  sudo ufw allow OpenSSH
  sudo ufw allow 80/tcp
  sudo ufw allow 443/tcp
  sudo ufw --force enable
fi

echo
echo "Done. Log out and back in so the docker group applies."
echo "Next: add VM_HOST, VM_USER, VM_SSH_KEY, VM_SSH_PORT and GH_STATS_TOKEN as GitHub secrets, then push to main."
