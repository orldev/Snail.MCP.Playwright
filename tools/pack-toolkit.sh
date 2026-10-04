#!/usr/bin/env bash
# Rebuilds the bundled Snail.Toolkit.Web packages from the toolkit's checkout.
#
#   tools/pack-toolkit.sh                 # the toolkit beside this repository
#   tools/pack-toolkit.sh /path/to/it     # or wherever it is checked out
#
# Run it after every change to the toolkit. The version never moves while the toolkit is unpublished,
# so NuGet would keep serving the copy it restored last time: the cached packages are removed here, and
# without that step a rebuilt package changes nothing.
set -euo pipefail

here="$(cd "$(dirname "$0")/.." && pwd)"
toolkit="${1:-${SNAIL_TOOLKIT_WEB:-$here/../../Toolkit/Snail.Toolkit.Web}}"
version="0.1.0-local"
projects=(Web Search Scraping Search.Browser Search.Browser.Agent Scraping.Agent)

if [ ! -f "$toolkit/Snail.Toolkit.Web.slnx" ]; then
  echo "no Snail.Toolkit.Web checkout at $toolkit" >&2
  exit 1
fi

rm -f "$here"/packages/Snail.Toolkit.Web*.nupkg

for project in "${projects[@]}"; do
  dotnet pack "$toolkit/src/$project/$project.csproj" -c Release -o "$here/packages" -p:Version="$version" --nologo -v quiet
done

rm -f "$here"/packages/*.snupkg

cache="$(dotnet nuget locals global-packages --list | sed 's/^global-packages: //')"
rm -rf "$cache"/snail.toolkit.web*/"$version"

dotnet restore "$here/Snail.MCP.Playwright.slnx" --force --nologo -v quiet

echo "packed $(ls "$here"/packages/Snail.Toolkit.Web*.nupkg | wc -l | tr -d ' ') packages at $version from $(git -C "$toolkit" rev-parse --short HEAD)"
