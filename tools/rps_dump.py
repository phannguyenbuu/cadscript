#!/usr/bin/env python3
"""Extract readable parameters from a 3ds Max .rps render preset file."""

from __future__ import annotations

import argparse
import hashlib
import re
from collections import OrderedDict
from datetime import datetime, timezone
from pathlib import Path

import olefile


ASCII_MIN = 3
UTF16_MIN = 3


def extract_ascii_strings(data: bytes, min_len: int = ASCII_MIN) -> list[str]:
    out: list[str] = []
    buf = bytearray()
    for b in data:
        if b == 9 or 32 <= b <= 126:
            buf.append(b)
        else:
            if len(buf) >= min_len:
                out.append(buf.decode("latin-1"))
            buf.clear()
    if len(buf) >= min_len:
        out.append(buf.decode("latin-1"))
    return out


def extract_utf16le_ascii_strings(data: bytes, min_len: int = UTF16_MIN) -> list[str]:
    pattern = re.compile(rb"(?:[\x09\x20-\x7e]\x00){%d,}" % min_len)
    out: list[str] = []
    for m in pattern.finditer(data):
        s = m.group(0).decode("utf-16le", errors="ignore")
        if s:
            out.append(s)
    return out


def dedupe_keep_order(items: list[str]) -> list[str]:
    return list(OrderedDict((s, None) for s in items).keys())


def _has_obvious_gibberish_chunk(s: str) -> bool:
    # Reject chunks like 'uuu', '===', 'ZZZ', etc.
    return bool(re.search(r"(.)\1\1", s))


def is_key_value_strict(s: str) -> bool:
    if "=" not in s:
        return False
    if len(s) > 500:
        return False
    if s.count("=") != 1:
        return False
    left, _, right = s.partition("=")
    left = left.strip()
    right = right.strip()
    if not left or not right:
        return False
    if not re.match(r"^[A-Za-z][A-Za-z0-9_ ./#%\-:+]{1,120}$", left):
        return False
    if not re.search(r"[A-Za-z0-9]", right):
        return False
    if _has_obvious_gibberish_chunk(left):
        return False
    return True


def is_key_value_loose(s: str) -> bool:
    if "=" not in s:
        return False
    if len(s) > 500:
        return False
    if s.count("=") > 10:
        return False
    left, _, right = s.partition("=")
    left = left.strip()
    right = right.strip()
    if not left or not right:
        return False
    return bool(re.match(r"^[A-Za-z0-9_ ./#%()\-:+]+$", left))


def extract_stream_strings(data: bytes) -> dict[str, list[str]]:
    ascii_strings = extract_ascii_strings(data)
    utf16_strings = extract_utf16le_ascii_strings(data)
    merged = dedupe_keep_order(utf16_strings + ascii_strings)

    keyvals = [s for s in merged if is_key_value_strict(s)]
    loose_keyvals = [s for s in merged if is_key_value_loose(s)]
    numeric_tokens = [
        s
        for s in merged
        if re.match(r"^[A-Za-z0-9_ .\-:/()]{1,120}$", s) and "=" not in s and len(s) >= 3
    ]

    return {
        "keyvals": keyvals,
        "loose_keyvals": loose_keyvals,
        "all_strings": merged,
        "simple_tokens": numeric_tokens,
    }


def format_dump(input_path: Path) -> str:
    data = input_path.read_bytes()
    file_hash = hashlib.sha256(data).hexdigest()

    lines: list[str] = []
    lines.append("3ds Max Render Preset Dump")
    lines.append("=" * 80)
    lines.append(f"Input File : {input_path}")
    lines.append(f"File Size  : {len(data)} bytes")
    lines.append(f"SHA256     : {file_hash}")
    lines.append(f"Generated  : {datetime.now(timezone.utc).isoformat()} (UTC)")
    lines.append("")

    with olefile.OleFileIO(str(input_path)) as ole:
        entries = ole.listdir(streams=True, storages=False)
        lines.append("Streams")
        lines.append("-" * 80)
        for entry in entries:
            stream_name = "/".join(entry)
            stream_data = ole.openstream(entry).read()
            lines.append(f"- {stream_name} ({len(stream_data)} bytes)")
        lines.append("")

        global_keyvals: list[tuple[str, str]] = []
        for entry in entries:
            stream_name = "/".join(entry)
            stream_data = ole.openstream(entry).read()
            parsed = extract_stream_strings(stream_data)
            for kv in parsed["keyvals"]:
                global_keyvals.append((stream_name, kv))

        lines.append("Key=Value Candidates (deduped)")
        lines.append("-" * 80)
        seen = set()
        for stream_name, kv in global_keyvals:
            key = (stream_name, kv)
            if key in seen:
                continue
            seen.add(key)
            lines.append(f"[{stream_name}] {kv}")
        if not seen:
            lines.append("(none)")
        lines.append("")

        for entry in entries:
            stream_name = "/".join(entry)
            stream_data = ole.openstream(entry).read()
            parsed = extract_stream_strings(stream_data)

            lines.append(f"Stream: {stream_name}")
            lines.append("-" * 80)
            lines.append(f"Size: {len(stream_data)} bytes")
            lines.append(f"Key=Value count: {len(parsed['keyvals'])}")
            lines.append(f"Loose key=value-like count: {len(parsed['loose_keyvals'])}")
            lines.append(f"Readable token count: {len(parsed['all_strings'])}")
            lines.append("")

            lines.append("Key=Value")
            lines.append("~" * 80)
            if parsed["keyvals"]:
                for kv in parsed["keyvals"]:
                    lines.append(kv)
            else:
                lines.append("(none)")
            lines.append("")

            weak = [kv for kv in parsed["loose_keyvals"] if kv not in parsed["keyvals"]]
            lines.append("Low-Confidence Key=Value-Like Strings")
            lines.append("~" * 80)
            if weak:
                for kv in weak:
                    lines.append(kv)
            else:
                lines.append("(none)")
            lines.append("")

            lines.append("Readable Tokens")
            lines.append("~" * 80)
            for s in parsed["all_strings"]:
                lines.append(s)
            lines.append("")

    return "\n".join(lines) + "\n"


def default_output_path(input_path: Path) -> Path:
    return input_path.with_name(f"{input_path.stem}_dump.txt")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path, help="Path to .rps file")
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        default=None,
        help="Output text file path (default: <input_stem>_dump.txt)",
    )
    args = parser.parse_args()

    input_path = args.input.expanduser().resolve()
    output_path = args.output.expanduser().resolve() if args.output else default_output_path(input_path)

    if not input_path.exists():
        raise FileNotFoundError(f"Input not found: {input_path}")

    dump_text = format_dump(input_path)
    output_path.write_text(dump_text, encoding="utf-8")
    print(str(output_path))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
