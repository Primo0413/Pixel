#!/usr/bin/env python3
"""将 Assets/color.txt 转换为 color-aliases.json（仅保留编号与色号）。"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_color_txt(path: Path) -> dict[str, str]:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    result: dict[str, str] = {}

    for line in lines[1:]:  # 跳过表头
        line = line.strip()
        if not line:
            continue

        parts = line.split("\t")
        if len(parts) < 2:
            parts = line.split()
            if len(parts) < 2:
                continue

        code = parts[0].strip()
        hex_code = parts[1].strip().upper().lstrip("#")
        if not code or not hex_code:
            continue

        result[code] = f"#{hex_code}"

    return result


def main() -> None:
    parser = argparse.ArgumentParser(description="Convert palette TXT to color-aliases.json")
    parser.add_argument("--input", default="Assets/color.txt", help="Input TXT file path")
    parser.add_argument("--output", default="color-aliases.json", help="Output JSON file path")
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)

    if not input_path.exists():
        raise FileNotFoundError(f"Input file not found: {input_path}")

    data = parse_color_txt(input_path)
    output_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Done: wrote {len(data)} entries to {output_path}")


if __name__ == "__main__":
    main()
