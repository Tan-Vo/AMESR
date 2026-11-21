#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DATA="${UNITY_DATA:-$HOME/Unity/Hub/Editor/6000.5.7f1/Editor/Data}"
CSC="$(find "$DATA/DotNetSdk/sdk" -path '*/Roslyn/bincore/csc.dll' -print -quit)"
REF="$(find "$DATA/DotNetSdk/packs/Microsoft.NETCore.App.Ref" -type d -path '*/ref/net8.0' -print -quit)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
args=(-nologo -target:exe -langversion:latest -out:"$OUT/CoreTests.dll")
while IFS= read -r -d '' dll; do args+=("-r:$dll"); done < <(find "$REF" -name '*.dll' -print0)
CORE="$DATA/Managed/UnityEngine/UnityEngine.CoreModule.dll"
args+=("-r:$CORE" "$ROOT/Assets/Scripts/Utilities/Timer.cs" "$ROOT/Assets/Scripts/Utilities/WeightedSelection.cs" "$ROOT/Assets/Scripts/Core/SaveData.cs" "$ROOT/Assets/Scripts/Player/OxygenMath.cs" "$ROOT/Assets/Scripts/Diving/TensionMath.cs" "$ROOT/Tools/Tests/CoreTests.cs")
"$DATA/DotNetSdk/dotnet" "$CSC" "${args[@]}"
cp "$CORE" "$OUT/"
cp "$ROOT/Tools/Tests/CoreTests.runtimeconfig.json" "$OUT/"
"$DATA/DotNetSdk/dotnet" "$OUT/CoreTests.dll"
