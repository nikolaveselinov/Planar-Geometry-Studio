#!/usr/bin/env bash
# Detached signatures verify download bytes without changing system trust.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
rid="${1:?Pass a Linux or macOS runtime identifier}"
version="${2:-$(tr -d '[:space:]' < "$root/VERSION")}"
case "$rid" in
  linux-x64|linux-arm64) extensions=(tar.gz deb rpm run) ;;
  osx-x64|osx-arm64) extensions=(zip pkg dmg) ;;
  *) echo "Unsupported runtime: $rid" >&2; exit 2 ;;
esac
command -v gpg >/dev/null || { echo 'Install GnuPG to sign release downloads.' >&2; exit 1; }
stem="$root/artifacts/PlanarGeometryStudio-v$version-$rid"
for ext in "${extensions[@]}"; do test -s "$stem.$ext"; done
umask 077
work="$(mktemp -d)"
cleanup() {
  gpgconf --homedir "$work/sign" --kill all >/dev/null 2>&1 || true
  gpgconf --homedir "$work/verify" --kill all >/dev/null 2>&1 || true
  rm -rf "$work"
}
trap cleanup EXIT
mkdir -m 700 "$work/sign" "$work/verify"
signer='Nikola Veselinov (Planar Geometry Studio self-signed release)'
gpg --homedir "$work/sign" --batch --pinentry-mode loopback --passphrase '' \
  --quick-generate-key "$signer" rsa3072 sign 5y
fingerprint="$(gpg --homedir "$work/sign" --batch --with-colons --list-secret-keys | awk -F: '$1 == "fpr" { print $10; exit }')"
test -n "$fingerprint"
gpg --homedir "$work/sign" --batch --armor --export "$fingerprint" > "$stem-publisher.asc"
gpg --homedir "$work/verify" --batch --import "$stem-publisher.asc"
for ext in "${extensions[@]}"; do
  asset="$stem.$ext"
  gpg --homedir "$work/sign" --batch --yes --pinentry-mode loopback --passphrase '' \
    --local-user "$fingerprint" --digest-algo SHA256 --armor --detach-sign "$asset"
  gpg --homedir "$work/verify" --batch --status-fd 1 --verify "$asset.asc" "$asset" > "$work/status"
  awk -v key="$fingerprint" '$1 == "[GNUPG:]" && $2 == "VALIDSIG" && $3 == key { valid=1 } END { exit !valid }' "$work/status"
  # A signature must reject a modified payload, not merely parse successfully.
  cp "$asset" "$work/modified"
  printf '\\nModified verification fixture\\n' >> "$work/modified"
  if gpg --homedir "$work/verify" --batch --verify "$asset.asc" "$work/modified" >/dev/null 2>&1; then
    echo "Signature accepted a modified $ext payload." >&2; exit 1
  fi
  rm "$work/modified"
done
cat > "$stem-signatures.txt" <<TXT
Signer: $signer
Public key fingerprint: $fingerprint
Digest: SHA-256; signing key: RSA 3072
Download the matching asset, its .asc signature, and this runtime's -publisher.asc.
With GnuPG installed:
  gpg --import "$(basename "$stem")-publisher.asc"
  gpg --verify "<asset filename>.asc" "<asset filename>"
A good signature checks bytes against the supplied public key. The signer name
is self-declared, not a certificate-authority verified identity. Obtain all files
from the official GitHub release. A replacement key and signature can accompany
a replacement payload, so these do not authenticate a compromised release page.
Keys are generated separately for each build and are not stable across releases.
Only public keys are published; temporary private keys are deleted after signing.
These signatures do not add Apple notarization, Gatekeeper approval, apt/rpm
repository trust, or change system trust stores. Verification needs no admin rights.
TXT
chmod 644 "$stem-publisher.asc" "$stem-signatures.txt"
for ext in "${extensions[@]}"; do chmod 644 "$stem.$ext.asc"; done
echo "Verified $rid downloads signed by $signer ($fingerprint)."
