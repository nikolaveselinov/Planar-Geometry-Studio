#!/usr/bin/env python3
"""Keep primary downloads visible and bundle optional verification files."""
import argparse
import hashlib
from pathlib import Path
import zipfile


def prepare(source: Path, output: Path, version: str) -> None:
    stem = f'PlanarGeometryStudio-v{version}'
    formats = {'win-x64': ['-setup.exe', '.zip'], 'win-arm64': ['-setup.exe', '.zip'],
               'linux-x64': ['.deb', '.rpm', '.run', '.tar.gz'],
               'linux-arm64': ['.deb', '.rpm', '.run', '.tar.gz'],
               'osx-x64': ['.pkg', '.dmg', '.zip'], 'osx-arm64': ['.pkg', '.dmg', '.zip']}
    packages = [source / f'{stem}-{rid}{ext}' for rid, extensions in formats.items() for ext in extensions]
    metadata = []
    for rid, extensions in formats.items():
        if rid.startswith('win-'):
            continue
        metadata.extend(source / f'{stem}-{rid}{ext}.asc' for ext in extensions)
        metadata.extend([source / f'{stem}-{rid}-publisher.asc', source / f'{stem}-{rid}-signatures.txt'])
    expected = packages + metadata
    for path in expected:
        if not path.is_file() or path.stat().st_size == 0:
            raise ValueError(f'Missing release file: {path.name}')
    unexpected = {path.name for path in source.iterdir() if path.is_file()} - {p.name for p in expected}
    if unexpected:
        raise ValueError(f'Unexpected release files: {sorted(unexpected)}')
    output.mkdir(parents=True, exist_ok=True)
    if any(output.iterdir()):
        raise ValueError('Release output directory must be empty.')

    def checksum(path: Path) -> str:
        with path.open('rb') as stream:
            return f'{hashlib.file_digest(stream, "sha256").hexdigest()}  {path.name}\n'

    # Hash full downloads before creating the archive; keys remain public-only.
    package_checksums = ''.join(checksum(path) for path in sorted(packages))
    bundle = output / f'{stem}-verification.zip'
    with zipfile.ZipFile(bundle, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for path in metadata:
            archive.write(path, path.name)
        archive.writestr('SHA256SUMS', package_checksums)
        archive.writestr('README.txt', f'''Optional download verification for Planar Geometry Studio {version}

The application does not require this archive. It contains detached signatures
for macOS/Linux downloads, public keys, and fingerprints. Extract it to a folder
called verification next to your downloaded installer or portable archive.

With GnuPG installed, use your exact runtime and filename (example):
  gpg --import verification/{stem}-osx-arm64-publisher.asc
  gpg --verify verification/{stem}-osx-arm64.pkg.asc {stem}-osx-arm64.pkg

Windows executables have embedded signatures; inspect Properties > Digital Signatures.
SHA256SUMS lists checksums for all 18 downloads. Compare with sha256sum on Linux,
shasum -a 256 on macOS, or Get-FileHash -Algorithm SHA256 in Windows PowerShell.

A good signature verifies the bytes against the supplied public key. Signer names
are self-declared, not publicly verified identities. Keys differ by runtime/build.
Obtain files from the official GitHub release: a replaced download, key, and
signature can still agree. This archive does not grant OS trust or notarization.
Only public keys are included; verification needs no administrator rights.
''')
    (output / 'SHA256SUMS').write_text(package_checksums + checksum(bundle))
    for path in packages:
        path.replace(output / path.name)
    print(f'Prepared {len(packages)} downloads, one verification archive, and checksums.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--version', required=True)
    parser.add_argument('--source', type=Path, default=Path('release-staging'))
    parser.add_argument('--output', type=Path, default=Path('release-assets'))
    args = parser.parse_args()
    prepare(args.source, args.output, args.version)
