#!/usr/bin/env python3
"""Validate and package a Unity Web build for itch.io, using only Python's stdlib."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from zipfile import ZIP_STORED, ZipFile


def package(source, output):
    if output.is_relative_to(source):
        raise ValueError('Write the archive outside the source build directory.')
    index = source / 'index.html'
    if not index.is_file():
        raise ValueError('Missing root index.html; build the Web player first.')
    html = index.read_text()
    if '{{{' in html:
        raise ValueError('Unexpanded Unity template; use the exported build.')
    files = sorted(p for p in source.rglob('*') if p.is_file() and p.name != '.DS_Store' and p.suffix != '.meta')
    names = {p.relative_to(source).as_posix() for p in files}
    for field in ['dataUrl', 'frameworkUrl', 'codeUrl']:
        if not re.search(rf"{field}:\s*'[^']+'", html):
            raise ValueError(f'Missing Unity build URL: {field}')
    references = re.findall(r"(?:src=\"|(?:data|framework|code)Url:\s*')[^'\"]*", html)
    for reference in references:
        path = reference.split('"', 1)[1] if reference.startswith('src=') else reference.split("'", 1)[1]
        if path.startswith('/') or '://' in path or path not in names:
            raise ValueError(f'Missing or non-relative build reference: {path}')
    if len(files) > 1000 or sum(p.stat().st_size for p in files) > 500_000_000:
        raise ValueError('Build exceeds itch.io HTML5 archive limits.')
    manifest = []
    for path in files:
        relative = path.relative_to(source).as_posix()
        if path.is_symlink() or len(relative) > 240 or path.stat().st_size > 200_000_000:
            raise ValueError(f'Unsupported archive entry: {relative}')
        manifest.append({'path': relative, 'bytes': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    output.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(output, 'w', compression=ZIP_STORED) as archive:
        for path in files:
            archive.write(path, path.relative_to(source).as_posix())
    with ZipFile(output) as archive:
        if archive.testzip() is not None or 'index.html' not in archive.namelist():
            raise ValueError('Archive verification failed.')
    info = {'archive': output.name, 'bytes': output.stat().st_size, 'sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'files': manifest}
    output.with_suffix('.manifest.json').write_text(json.dumps(info, indent=2) + '\n')
    print(json.dumps({k: v for k, v in info.items() if k != 'files'}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=Path('Builds/Web'))
    parser.add_argument('--output', type=Path, default=Path('Builds/Release/uninvited-web.zip'))
    args = parser.parse_args()
    try:
        package(args.source.resolve(), args.output.resolve())
    except ValueError as error:
        parser.error(str(error))


if __name__ == '__main__':
    main()
