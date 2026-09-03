#!/usr/bin/env python3
"""Generate Unity .meta files and pack a .unitypackage.

Unity assigns every asset a GUID stored in a sibling .meta file, and cross-asset
references are by GUID. Generating them here, deterministically from the asset
path, means the same file keeps the same GUID across machines and re-packs -
so re-importing an updated package replaces assets instead of duplicating them.

  python3 tools/make_unity_package.py --metas          # write .meta files only
  python3 tools/make_unity_package.py --package out.unitypackage
"""
import argparse
import hashlib
import io
import os
import pathlib
import tarfile

PROJECT = pathlib.Path(__file__).resolve().parent.parent
ASSETS = PROJECT / "Assets"

# Files Unity should never see as assets.
SKIP_NAMES = {".DS_Store", "Thumbs.db"}
SKIP_SUFFIXES = {".meta"}


def guid_for(relative_path: str) -> str:
    """Deterministic GUID: stable across machines, unique per asset path."""
    return hashlib.md5(f"caminafeliz:{relative_path}".encode("utf-8")).hexdigest()


def importer_block(path: pathlib.Path, is_dir: bool) -> str:
    if is_dir:
        return "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
    if path.suffix == ".cs":
        return (
            "MonoImporter:\n"
            "  externalObjects: {}\n"
            "  serializedVersion: 2\n"
            "  defaultReferences: []\n"
            "  executionOrder: 0\n"
            "  icon: {instanceID: 0}\n"
        )
    if path.suffix == ".asmdef":
        return "AssemblyDefinitionImporter:\n  externalObjects: {}\n"
    if path.suffix in (".txt", ".json", ".html"):
        return "TextScriptImporter:\n  externalObjects: {}\n"
    return "DefaultImporter:\n  externalObjects: {}\n"


def meta_text(path: pathlib.Path, relative: str, is_dir: bool) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid_for(relative)}\n"
        + importer_block(path, is_dir)
        + "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def walk_assets():
    """Yield (absolute path, 'Assets/...' path, is_dir) for everything importable."""
    for root, dirs, files in os.walk(ASSETS):
        dirs.sort()
        files.sort()
        root_path = pathlib.Path(root)

        if root_path != ASSETS:
            yield root_path, root_path.relative_to(PROJECT).as_posix(), True

        for name in files:
            if name in SKIP_NAMES or pathlib.Path(name).suffix in SKIP_SUFFIXES:
                continue
            file_path = root_path / name
            yield file_path, file_path.relative_to(PROJECT).as_posix(), False


def write_metas() -> int:
    written = 0
    for path, relative, is_dir in walk_assets():
        meta_path = path.parent / (path.name + ".meta")
        text = meta_text(path, relative, is_dir)
        if not meta_path.exists() or meta_path.read_text() != text:
            meta_path.write_text(text)
            written += 1
    return written


def build_package(output: pathlib.Path) -> int:
    """A .unitypackage is a gzipped tar of <guid>/{asset,asset.meta,pathname}."""
    count = 0
    with tarfile.open(output, "w:gz") as tar:
        for path, relative, is_dir in walk_assets():
            guid = guid_for(relative)

            def add(name: str, data: bytes):
                info = tarfile.TarInfo(f"{guid}/{name}")
                info.size = len(data)
                info.mode = 0o644
                tar.addfile(info, io.BytesIO(data))

            add("pathname", relative.encode("utf-8"))
            add("asset.meta", meta_text(path, relative, is_dir).encode("utf-8"))
            if not is_dir:
                add("asset", path.read_bytes())
            count += 1
    return count


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metas", action="store_true", help="write .meta files next to each asset")
    parser.add_argument("--package", metavar="FILE", help="write a .unitypackage")
    args = parser.parse_args()

    if not args.metas and not args.package:
        parser.error("nothing to do: pass --metas, --package, or both")

    if args.metas:
        print(f".meta escritos o actualizados: {write_metas()}")

    if args.package:
        out = pathlib.Path(args.package).resolve()
        print(f"assets empaquetados: {build_package(out)} -> {out}")
