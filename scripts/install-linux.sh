#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
install_directory="$HOME/.local/share/PrTray"
desktop_entry_path="$HOME/.local/share/applications/prtray.desktop"
autostart_entry_path="$HOME/.config/autostart/prtray.desktop"

pkill -x prtray || true

dotnet publish "$repository_root/src/PrTray.App/PrTray.App.csproj" \
  -c Release -r linux-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$install_directory"

mkdir -p "$(dirname "$desktop_entry_path")" "$(dirname "$autostart_entry_path")"
cat > "$desktop_entry_path" <<EOF
[Desktop Entry]
Name=PrTray
Comment=GitHub PR-varsler
Exec=$install_directory/prtray
Icon=emblem-default
Type=Application
Categories=Development;
EOF
cp "$desktop_entry_path" "$autostart_entry_path"
update-desktop-database "$(dirname "$desktop_entry_path")" 2>/dev/null || true

systemctl --user disable --now gh-pr-review-notifier.service 2>/dev/null || true

setsid -f "$install_directory/prtray" >/dev/null 2>&1
echo "PrTray installert i $install_directory og startet. Config: ~/.config/PrTray/config.json"
