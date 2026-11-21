#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DATA="${UNITY_DATA:-$HOME/Unity/Hub/Editor/6000.5.7f1/Editor/Data}"
CSC="$(find "$DATA/DotNetSdk/sdk" -path '*/Roslyn/bincore/csc.dll' -print -quit)"
REF="$(find "$DATA/DotNetSdk/packs/NETStandard.Library.Ref" -type d -path '*/ref/netstandard2.1' -print -quit)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
args=(-nologo -target:library -langversion:latest -define:UNITY_EDITOR -out:"$OUT/AMESR.dll")
while IFS= read -r -d '' dll; do args+=("-r:$dll"); done < <(find "$REF" "$DATA/Managed/UnityEngine" -maxdepth 1 -name '*.dll' -print0)
while IFS= read -r -d '' source; do args+=("$source"); done < <(find "$ROOT/Assets/Scripts" -name '*.cs' -print0 | sort -z)
"$DATA/DotNetSdk/dotnet" "$CSC" "${args[@]}"
echo "Compile check: 0 errors (all Assets/Scripts runtime + editor C#)."
