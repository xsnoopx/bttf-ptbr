#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Gera src/patch.bin: o diff entre o executavel original do jogo e o traduzido.

Uso:
    python build_payload.py <original.exe> <traduzido.exe> [patch.bin]

O patch so funciona na build publica cujo SHA-256 e o ORIG_SHA conhecido
(BackToTheFuture-Windows-7284fa5, baixada de playbacktothefuture.com).
O instalador valida esse hash antes de escrever qualquer byte.
"""
import hashlib
import json
import struct
import sys

BLOCK = 1 << 20     # compara em blocos de 1 MiB
MERGE_GAP = 64      # junta trechos separados por menos de 64 bytes iguais


def diff_runs(a, b):
    n = min(len(a), len(b))
    runs = []
    i = 0
    while i < n:
        end = min(i + BLOCK, n)
        if a[i:end] != b[i:end]:
            j = i
            while j < end and a[j] == b[j]:
                j += 1
            k = j
            while k < end and a[k] != b[k]:
                k += 1
            runs.append([j, k])
            i = k
        else:
            i = end
    merged = []
    for s, e in runs:
        if merged and s - merged[-1][1] < MERGE_GAP:
            merged[-1][1] = e
        else:
            merged.append([s, e])
    if len(b) > n:                      # bytes novos no fim (nova secao .pdpt)
        merged.append([n, len(b)])
    return merged


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1
    orig_path, new_path = sys.argv[1], sys.argv[2]
    out = sys.argv[3] if len(sys.argv) > 3 else "patch.bin"

    a = open(orig_path, "rb").read()
    b = open(new_path, "rb").read()
    runs = diff_runs(a, b)

    blob = bytearray(b"PDBR1\0\0\0")
    blob += hashlib.sha256(a).digest()      # hash esperado do original
    blob += hashlib.sha256(b).digest()      # hash esperado do resultado
    blob += struct.pack("<QQ", len(a), len(b))
    blob += struct.pack("<I", len(runs))
    for s, e in runs:
        blob += struct.pack("<QI", s, e - s) + b[s:e]

    open(out, "wb").write(bytes(blob))
    print(json.dumps({
        "patch": out,
        "bytes": len(blob),
        "runs": len(runs),
        "orig": {"size": len(a), "sha256": hashlib.sha256(a).hexdigest()},
        "patched": {"size": len(b), "sha256": hashlib.sha256(b).hexdigest()},
    }, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
