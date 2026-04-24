#!/usr/bin/env python3
"""Analyze adaptation CSV logs without external dependencies.

Usage:
    python3 scripts/analyze_adaptation_logs.py VRData/adaptation_data_20260317_134300.csv
    python3 scripts/analyze_adaptation_logs.py VRData/*.csv
"""

from __future__ import annotations

import csv
import math
import os
import statistics
import sys


LOW_VELOCITY_THRESHOLD = 30.0
HIGH_VELOCITY_THRESHOLD = 90.0
MIN_MULTIPLIER = 0.8
MAX_MULTIPLIER = 1.2


def load_rows(path: str) -> list[dict[str, float]]:
    with open(path, newline="", encoding="utf-8-sig") as handle:
        reader = csv.DictReader(handle)
        rows = []
        for row in reader:
            rows.append(
                {
                    "time": float(row["Time"]),
                    "base": float(row["OverstimulationLevel"]),
                    "adapted": float(row["AdaptedLevel"]),
                    "multiplier": float(row["AdaptationMultiplier"]),
                    "rotation": float(row["HeadRotationSpeed"]),
                    "discomfort": float(row["DiscomfortLevel"]),
                    "active_triggers": float(row["ActiveTriggers"]),
                }
            )
    return rows


def active_phase_rows(rows: list[dict[str, float]]) -> list[dict[str, float]]:
    """Keep only the stimulation phase where the trigger zone is active."""
    active = [row for row in rows if row["active_triggers"] > 0.0]
    return active if active else rows


def derivative(values: list[float], times: list[float]) -> list[float]:
    output = []
    for index in range(1, len(values)):
        dt = times[index] - times[index - 1]
        if dt <= 0:
            output.append(0.0)
            continue
        output.append((values[index] - values[index - 1]) / dt)
    return output


def sign(value: float, epsilon: float = 1e-6) -> int:
    if value > epsilon:
        return 1
    if value < -epsilon:
        return -1
    return 0


def max_sign_changes_per_window(slopes: list[float], times: list[float], window_seconds: float = 1.0) -> int:
    if len(slopes) < 2:
        return 0

    signs = [sign(slope) for slope in slopes]
    max_changes = 0
    for start in range(len(signs)):
        changes = 0
        last = signs[start]
        for current in range(start + 1, len(signs)):
            if times[current + 1] - times[start + 1] > window_seconds:
                break
            if signs[current] != 0 and last != 0 and signs[current] != last:
                changes += 1
            if signs[current] != 0:
                last = signs[current]
        max_changes = max(max_changes, changes)
    return max_changes


def summarize(path: str) -> None:
    all_rows = load_rows(path)
    if not all_rows:
        print(f"{path}: no rows")
        return

    rows = active_phase_rows(all_rows)

    times = [row["time"] for row in rows]
    base = [row["base"] for row in rows]
    adapted = [row["adapted"] for row in rows]
    multiplier = [row["multiplier"] for row in rows]
    rotation = [row["rotation"] for row in rows]
    discomfort = [row["discomfort"] for row in rows]

    slopes = derivative(adapted, times)
    non_negative_ratio = sum(1 for slope in slopes if slope >= -1e-6) / len(slopes) if slopes else float("nan")
    mean_slope = statistics.mean(slopes) if slopes else float("nan")

    low_velocity_samples = [row for row in rows if row["rotation"] < LOW_VELOCITY_THRESHOLD]
    high_velocity_samples = [row for row in rows if row["rotation"] >= HIGH_VELOCITY_THRESHOLD]

    adapted_below_base_ratio = (
        sum(1 for row in high_velocity_samples if row["adapted"] < row["base"]) / len(high_velocity_samples)
        if high_velocity_samples
        else float("nan")
    )
    discomfort_high_ratio = (
        sum(1 for row in high_velocity_samples if row["discomfort"] >= 0.95) / len(high_velocity_samples)
        if high_velocity_samples
        else float("nan")
    )

    bounded_levels = all(0.0 <= value <= 1.0 for value in base + adapted)
    bounded_multiplier = all(MIN_MULTIPLIER - 1e-4 <= value <= MAX_MULTIPLIER + 1e-4 for value in multiplier)

    print(f"\nFile: {path}")
    print(f"Rows (full run): {len(all_rows)}")
    print(f"Rows (active phase): {len(rows)}")
    print(f"Active phase filtered: {len(rows) != len(all_rows)}")
    print(f"Duration: {times[-1] - times[0]:.3f}s")
    print(f"Active phase start: {times[0]:.3f}s")
    print(f"Active phase end: {times[-1]:.3f}s")
    print(f"Base level range: {min(base):.4f} to {max(base):.4f}")
    print(f"Adapted level range: {min(adapted):.4f} to {max(adapted):.4f}")
    print(f"Multiplier range: {min(multiplier):.4f} to {max(multiplier):.4f}")
    print(f"Rotation range: {min(rotation):.2f} to {max(rotation):.2f} deg/s")
    print(f"Discomfort range: {min(discomfort):.4f} to {max(discomfort):.4f}")
    print(f"Low-velocity samples (<30 deg/s): {len(low_velocity_samples)}")
    print(f"High-velocity samples (>=90 deg/s): {len(high_velocity_samples)}")
    print(f"Accumulation metric - non-negative derivative ratio: {non_negative_ratio:.3f}")
    print(f"Accumulation metric - mean adapted slope: {mean_slope:.6f}")
    print(f"Safety metric - high-discomfort ratio: {discomfort_high_ratio:.3f}")
    print(f"Safety metric - adapted below base ratio in rapid regime: {adapted_below_base_ratio:.3f}")
    print(f"Boundedness - levels valid: {bounded_levels}")
    print(f"Boundedness - multiplier valid: {bounded_multiplier}")
    print(f"Stability - max derivative sign changes in 1s window: {max_sign_changes_per_window(slopes, times)}")

    if max(rotation) == 0.0:
        print("Warning: rotation never exceeded 0.0. Logger setup may still be incorrect for this run.")


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__.strip())
        return 1

    for path in argv[1:]:
        if not os.path.exists(path):
            print(f"Missing file: {path}")
            return 1
        summarize(path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
