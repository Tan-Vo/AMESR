#!/usr/bin/env python3
"""Create missing Unity metadata without changing existing GUIDs."""
import pathlib
import uuid

root = pathlib.Path(__file__).resolve().parents[1] / "Assets"
for path in sorted(root.rglob("*")):
    if not (path.is_dir() or path.suffix == ".cs"):
        continue
    meta = pathlib.Path(str(path) + ".meta")
    if meta.exists():
        continue
    header = f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n"
    importer = ("folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                if path.is_dir() else
                "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    meta.write_text("\n".join(line.rstrip() for line in (header + importer).splitlines()) + "\n")
