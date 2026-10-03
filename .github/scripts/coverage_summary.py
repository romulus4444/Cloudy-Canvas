#!/usr/bin/env python3
"""Summarise a Cobertura coverage report (what coverlet.collector writes) as a markdown table.

Usage: coverage_summary.py REPORT.xml [--threshold PERCENT]

Prints line coverage per class, least covered first, and the total. With --threshold the exit code is 1 when the total line coverage
is below PERCENT, so CI can stop coverage from quietly falling. Only the standard library is used.
"""
import sys
import xml.etree.ElementTree as ET


def class_name(name: str) -> str:
    # Compiler-generated nested types ("Outer/<Inner>d__3", "Outer+<>c") count towards the class they belong to.
    for marker in ("/", "+", "<"):
        name = name.split(marker)[0]
    return name


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2

    report = argv[1]
    threshold = None
    if "--threshold" in argv:
        threshold = float(argv[argv.index("--threshold") + 1])

    root = ET.parse(report).getroot()
    per_class = {}
    for cls in root.iter("class"):
        name = class_name(cls.get("name"))
        covered, total = per_class.get(name, (0, 0))
        lines = {}
        for line in cls.iter("line"):
            number = int(line.get("number"))
            lines[number] = max(lines.get(number, 0), int(line.get("hits")))
        per_class[name] = (covered + sum(1 for hits in lines.values() if hits > 0), total + len(lines))

    covered_all = sum(c for c, _ in per_class.values())
    total_all = sum(t for _, t in per_class.values())
    percent_all = 100.0 * covered_all / total_all if total_all else 100.0

    print("| Class | Lines covered | Coverage |")
    print("| --- | ---: | ---: |")
    for name, (covered, total) in sorted(per_class.items(), key=lambda item: (item[1][0] / item[1][1] if item[1][1] else 1, item[0])):
        if total == 0:
            continue
        print(f"| `{name}` | {covered}/{total} | {100.0 * covered / total:.1f}% |")
    print(f"| **Total** | **{covered_all}/{total_all}** | **{percent_all:.1f}%** |")

    if threshold is not None and percent_all < threshold:
        print(f"\nLine coverage {percent_all:.1f}% is below the required {threshold:.1f}%.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
