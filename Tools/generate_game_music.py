#!/usr/bin/env python3
"""Generate RoboCarl looping BGM WAVs (menu + level).

Produces mono 16-bit 44.1 kHz clips with multiple coordinated instruments
and unique material for the full loop length (no copy-paste repeats).
"""

from __future__ import annotations

import math
import wave
from pathlib import Path

import numpy as np

SR = 44100
TWO_PI = 2.0 * math.pi

# MIDI note helpers
NOTE = {
    "C2": 36, "D2": 38, "E2": 40, "F2": 41, "G2": 43, "A2": 45, "B2": 47,
    "C3": 48, "D3": 50, "E3": 52, "F3": 53, "G3": 55, "A3": 57, "B3": 59,
    "C4": 60, "D4": 62, "E4": 64, "F4": 65, "G4": 67, "A4": 69, "B4": 71,
    "C5": 72, "D5": 74, "E5": 76, "F5": 77, "G5": 79, "A5": 81, "B5": 83,
    "C6": 84,
}


def midi_to_hz(m: float) -> float:
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))


def note_name_to_hz(name: str) -> float:
    return midi_to_hz(NOTE[name])


def adsr(n: int, attack: float, decay: float, sustain: float, release: float, peak: float = 1.0) -> np.ndarray:
    env = np.zeros(n, dtype=np.float32)
    a = int(attack * SR)
    d = int(decay * SR)
    r = int(release * SR)
    s_len = max(0, n - a - d - r)
    i = 0
    if a > 0:
        env[i : i + a] = np.linspace(0.0, peak, a, endpoint=False, dtype=np.float32)
        i += a
    elif n:
        env[0] = peak
    if d > 0 and i < n:
        seg = min(d, n - i)
        env[i : i + seg] = np.linspace(peak, peak * sustain, seg, endpoint=False, dtype=np.float32)
        i += seg
    if s_len > 0 and i < n:
        seg = min(s_len, n - i)
        env[i : i + seg] = peak * sustain
        i += seg
    if r > 0 and i < n:
        seg = n - i
        start = env[i - 1] if i > 0 else peak * sustain
        env[i:] = np.linspace(start, 0.0, seg, dtype=np.float32)
    return env


def soft_clip(x: np.ndarray, drive: float = 1.15) -> np.ndarray:
    return np.tanh(x * drive).astype(np.float32)


def render_sine(freq: float, n: int, phase0: float = 0.0) -> tuple[np.ndarray, float]:
    t = (np.arange(n, dtype=np.float64) / SR)
    phase = phase0 + TWO_PI * freq * t
    return np.sin(phase).astype(np.float32), float(phase[-1] % TWO_PI) if n else phase0


def render_triangle(freq: float, n: int, phase0: float = 0.0) -> tuple[np.ndarray, float]:
    t = np.arange(n, dtype=np.float64) / SR
    phase = (phase0 / TWO_PI + freq * t) % 1.0
    tri = 2.0 * np.abs(2.0 * phase - 1.0) - 1.0
    return tri.astype(np.float32), float((phase[-1] * TWO_PI) % TWO_PI) if n else phase0


def render_square(freq: float, n: int, duty: float = 0.5, phase0: float = 0.0) -> tuple[np.ndarray, float]:
    t = np.arange(n, dtype=np.float64) / SR
    phase = (phase0 / TWO_PI + freq * t) % 1.0
    sq = np.where(phase < duty, 1.0, -1.0).astype(np.float32)
    # gentle lowpass via moving average to tame harshness
    k = 12
    ker = np.ones(k, dtype=np.float32) / k
    sq = np.convolve(sq, ker, mode="same")
    return sq.astype(np.float32), float((phase[-1] * TWO_PI) % TWO_PI) if n else phase0


def render_pulse_soft(freq: float, n: int, duty: float = 0.35) -> np.ndarray:
    wave, _ = render_square(freq, n, duty=duty)
    # blend a little sine for warmer chip-lead
    sine, _ = render_sine(freq, n)
    return (0.72 * wave + 0.28 * sine).astype(np.float32)


def render_bell(freq: float, n: int) -> np.ndarray:
    # soft FM-ish bell / mallet
    t = np.arange(n, dtype=np.float64) / SR
    mod = np.sin(TWO_PI * freq * 2.01 * t) * np.exp(-3.5 * t)
    car = np.sin(TWO_PI * freq * t + 1.6 * mod)
    partial = 0.35 * np.sin(TWO_PI * freq * 3.0 * t) * np.exp(-5.0 * t)
    return (car + partial).astype(np.float32)


def place(buf: np.ndarray, start: int, signal: np.ndarray, gain: float = 1.0) -> None:
    if start >= len(buf) or len(signal) == 0:
        return
    end = min(len(buf), start + len(signal))
    sl = end - start
    buf[start:end] += signal[:sl] * gain


def beats_to_samples(beats: float, bpm: float) -> int:
    return int(round(beats * 60.0 / bpm * SR))


def write_wav(path: Path, audio: np.ndarray) -> None:
    audio = soft_clip(audio, 1.05)
    peak = float(np.max(np.abs(audio))) or 1.0
    audio = (audio / peak * 0.88).astype(np.float32)
    # tiny fade at ends for seamless loop
    fade = int(0.012 * SR)
    if fade * 2 < len(audio):
        audio[:fade] *= np.linspace(0.0, 1.0, fade, dtype=np.float32)
        audio[-fade:] *= np.linspace(1.0, 0.0, fade, dtype=np.float32)
        # cross-ish loop: mix start into end slightly already faded
    pcm = np.clip(audio * 32767.0, -32768, 32767).astype(np.int16)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"Wrote {path}  duration={len(audio)/SR:.3f}s  peak={peak:.3f}")


def schedule_notes(
    buf: np.ndarray,
    events: list[tuple[float, str | None, float]],
    bpm: float,
    instrument: str,
    gain: float,
) -> None:
    """events: (beat, note_name|None rest, duration_beats)."""
    for beat, note, dur in events:
        if note is None:
            continue
        start = beats_to_samples(beat, bpm)
        n = beats_to_samples(dur, bpm)
        if n <= 0 or start >= len(buf):
            continue
        n = min(n, len(buf) - start)
        freq = note_name_to_hz(note)
        if instrument == "bass_tri":
            raw, _ = render_triangle(freq, n)
            env = adsr(n, 0.01, 0.08, 0.7, min(0.12, dur * 60 / bpm * 0.35))
            sig = raw * env
        elif instrument == "bass_sine":
            raw, _ = render_sine(freq, n)
            sub, _ = render_sine(freq * 0.5, n)
            env = adsr(n, 0.008, 0.1, 0.75, min(0.14, dur * 60 / bpm * 0.4))
            sig = (0.85 * raw + 0.25 * sub) * env
        elif instrument == "lead_pulse":
            raw = render_pulse_soft(freq, n, duty=0.38)
            env = adsr(n, 0.006, 0.05, 0.55, min(0.1, dur * 60 / bpm * 0.3), peak=1.0)
            sig = raw * env
        elif instrument == "lead_square":
            raw, _ = render_square(freq, n, duty=0.5)
            env = adsr(n, 0.004, 0.04, 0.5, min(0.08, dur * 60 / bpm * 0.28))
            sig = raw * env
        elif instrument == "bell":
            raw = render_bell(freq, n)
            env = adsr(n, 0.002, 0.12, 0.25, min(0.35, max(0.08, dur * 60 / bpm * 0.55)))
            sig = raw * env
        elif instrument == "pad":
            # longer pad tone; ignore short note envelopes somewhat
            raw, _ = render_sine(freq, n)
            fifth, _ = render_sine(freq * (3 / 2) * 0.997, n)
            raw2, _ = render_triangle(freq * 0.5, n)
            env = adsr(n, 0.18, 0.25, 0.7, min(0.4, dur * 60 / bpm * 0.45))
            sig = (0.55 * raw + 0.25 * fifth + 0.3 * raw2) * env
        else:
            raise ValueError(instrument)
        place(buf, start, sig, gain=gain)


def arp_pattern(chords: list[list[str]], start_beat: float, bars: int, bpm: float, pattern: list[int]) -> list[tuple[float, str | None, float]]:
    """16th-note arpeggio from chord tones. pattern indexes into chord."""
    events = []
    step = 0.25  # 16th at 4/4
    for bar in range(bars):
        chord = chords[bar % len(chords)]
        for i in range(16):
            beat = start_beat + bar * 4 + i * step
            idx = pattern[i % len(pattern)]
            if idx < 0:
                events.append((beat, None, step))
            else:
                events.append((beat, chord[idx % len(chord)], step * 0.95))
    return events


def melody_events(notes: list[tuple[float, str | None, float]], start_beat: float = 0.0) -> list[tuple[float, str | None, float]]:
    return [(start_beat + b, n, d) for b, n, d in notes]


def build_menu_track() -> np.ndarray:
    """Calm, warm menu loop: bass + bell melody (+ pad beds). Unique ~36s+."""
    bpm = 108.0
    # 20 bars * 4 beats = 80 beats ≈ 44.44s — well over 3x 8.96s
    total_bars = 20
    total_beats = total_bars * 4
    n = beats_to_samples(total_beats, bpm)
    buf = np.zeros(n, dtype=np.float32)

    # Chord progression (unique sections, not one loop thrice)
    # Bars 0-3: Am - F - C - G
    # Bars 4-7: Am - Em - F - G
    # Bars 8-11: Dm - Am - F - E
    # Bars 12-15: Am - F - C - G  (melody developed)
    # Bars 16-19: F - G - Am - Am (resolve / loop back)
    bass_roots = (
        ["A2", "F2", "C3", "G2"]
        + ["A2", "E2", "F2", "G2"]
        + ["D2", "A2", "F2", "E2"]
        + ["A2", "F2", "C3", "G2"]
        + ["F2", "G2", "A2", "A2"]
    )
    bass_events = []
    for bar, root in enumerate(bass_roots):
        # half notes with a fifth on beat 3 for motion
        bass_events.append((bar * 4 + 0.0, root, 1.9))
        fifth = {
            "A2": "E3", "F2": "C3", "C3": "G3", "G2": "D3",
            "E2": "B2", "D2": "A2",
        }[root]
        bass_events.append((bar * 4 + 2.0, fifth, 1.8))

    # Melody A (bars 0-3) — call
    mel_a = [
        (0.0, "E4", 1.0), (1.0, "A4", 1.0), (2.0, "C5", 1.0), (3.0, "B4", 1.0),
        (4.0, "A4", 1.5), (5.5, "G4", 0.5), (6.0, "E4", 1.0), (7.0, "C4", 1.0),
        (8.0, "D4", 1.0), (9.0, "E4", 1.0), (10.0, "G4", 1.5), (11.5, "E4", 0.5),
        (12.0, "D4", 2.0), (14.0, "B3", 2.0),
    ]
    # Melody B (bars 4-7) — answer / higher
    mel_b = [
        (0.0, "A4", 0.5), (0.5, "B4", 0.5), (1.0, "C5", 1.0), (2.0, "E5", 1.0), (3.0, "D5", 1.0),
        (4.0, "B4", 1.5), (5.5, "G4", 0.5), (6.0, "A4", 2.0),
        (8.0, "F4", 1.0), (9.0, "A4", 1.0), (10.0, "C5", 1.0), (11.0, "A4", 1.0),
        (12.0, "G4", 1.0), (13.0, "B4", 1.0), (14.0, "D5", 2.0),
    ]
    # Melody C (bars 8-11) — darker turn
    mel_c = [
        (0.0, "F4", 1.0), (1.0, "A4", 0.5), (1.5, "C5", 0.5), (2.0, "D5", 1.0), (3.0, "C5", 1.0),
        (4.0, "E4", 2.0), (6.0, "A4", 1.0), (7.0, "G4", 1.0),
        (8.0, "A4", 1.5), (9.5, "F4", 0.5), (10.0, "C5", 2.0),
        (12.0, "B4", 1.0), (13.0, "E4", 1.0), (14.0, "B3", 2.0),
    ]
    # Melody A' developed (bars 12-15)
    mel_ap = [
        (0.0, "E4", 0.5), (0.5, "G4", 0.5), (1.0, "A4", 1.0), (2.0, "C5", 0.5), (2.5, "B4", 0.5), (3.0, "A4", 1.0),
        (4.0, "F4", 1.0), (5.0, "A4", 1.0), (6.0, "C5", 1.0), (7.0, "E5", 1.0),
        (8.0, "G5", 1.5), (9.5, "E5", 0.5), (10.0, "C5", 1.0), (11.0, "G4", 1.0),
        (12.0, "B4", 2.0), (14.0, "D5", 2.0),
    ]
    # Melody D outro (bars 16-19) — settle for loop
    mel_d = [
        (0.0, "C5", 1.0), (1.0, "A4", 1.0), (2.0, "F4", 2.0),
        (4.0, "B4", 1.0), (5.0, "G4", 1.0), (6.0, "D4", 2.0),
        (8.0, "E4", 1.0), (9.0, "A4", 1.0), (10.0, "C5", 1.0), (11.0, "E4", 1.0),
        (12.0, "A4", 4.0),
    ]

    # Pads (sustained chord beds) — different instrument from bass/melody
    pad_chords = [
        (0, ["A3", "C4", "E4"], 4),
        (4, ["F3", "A3", "C4"], 4),
        (8, ["C3", "E3", "G3"], 4),
        (12, ["G3", "B3", "D4"], 4),
        (16, ["A3", "C4", "E4"], 4),
        (20, ["E3", "G3", "B3"], 4),
        (24, ["F3", "A3", "C4"], 4),
        (28, ["G3", "B3", "D4"], 4),
        (32, ["D3", "F3", "A3"], 4),
        (36, ["A3", "C4", "E4"], 4),
        (40, ["F3", "A3", "C4"], 4),
        (44, ["E3", "G3", "B3"], 4),
        (48, ["A3", "C4", "E4"], 4),
        (52, ["F3", "A3", "C4"], 4),
        (56, ["C3", "E3", "G3"], 4),
        (60, ["G3", "B3", "D4"], 4),
        (64, ["F3", "A3", "C4"], 4),
        (68, ["G3", "B3", "D4"], 4),
        (72, ["A3", "C4", "E4"], 8),
    ]
    pad_events = []
    for start, notes, dur in pad_chords:
        for note in notes:
            pad_events.append((float(start), note, float(dur)))

    schedule_notes(buf, bass_events, bpm, "bass_tri", gain=0.34)
    schedule_notes(buf, pad_events, bpm, "pad", gain=0.12)
    schedule_notes(buf, melody_events(mel_a, 0), bpm, "bell", gain=0.28)
    schedule_notes(buf, melody_events(mel_b, 16), bpm, "bell", gain=0.30)
    schedule_notes(buf, melody_events(mel_c, 32), bpm, "bell", gain=0.29)
    schedule_notes(buf, melody_events(mel_ap, 48), bpm, "bell", gain=0.30)
    schedule_notes(buf, melody_events(mel_d, 64), bpm, "bell", gain=0.27)

    # Soft counter-line on pulse in mid sections (instrument 2+3 with bass) — syncopated, not unison
    counter = [
        (16.0, "C4", 0.5), (17.0, "E4", 0.5), (18.5, "A3", 0.5), (19.5, "C4", 0.5),
        (20.0, "B3", 1.0), (22.0, "G3", 1.0),
        (24.0, "A3", 0.5), (25.0, "C4", 0.5), (26.5, "F3", 0.5), (27.5, "A3", 0.5),
        (28.0, "B3", 2.0),
        (48.0, "E4", 0.5), (49.5, "C4", 0.5), (51.0, "A3", 1.0),
        (52.0, "F3", 0.5), (53.5, "A3", 0.5), (55.0, "C4", 1.0),
        (56.0, "G3", 1.0), (58.0, "E3", 1.0), (60.0, "D4", 2.0),
    ]
    schedule_notes(buf, counter, bpm, "lead_pulse", gain=0.11)

    return buf


def build_level_track() -> np.ndarray:
    """Energetic level loop: driving bass + lead (+ arpeggio). Unique ~28s+."""
    bpm = 132.0
    # 18 bars ≈ 32.73s — well over 3x 7.04s
    total_bars = 18
    total_beats = total_bars * 4
    n = beats_to_samples(total_beats, bpm)
    buf = np.zeros(n, dtype=np.float32)

    # Sections:
    # 0-3 intro groove
    # 4-7 hook
    # 8-11 rise / alt harmony
    # 12-15 hook variant
    # 16-17 tag back to top
    bass_line = []
    # Pattern helpers: octave pump
    def add_bass_bar(bar: int, root: str, fifth: str, style: str = "pump") -> None:
        b0 = bar * 4
        if style == "pump":
            for i, note in enumerate([root, root, fifth, root]):
                bass_line.append((b0 + i, note, 0.85))
        elif style == "gallop":
            for off, note, dur in [
                (0.0, root, 0.45), (0.5, root, 0.45), (1.0, fifth, 0.9),
                (2.0, root, 0.45), (2.5, fifth, 0.45), (3.0, root, 0.9),
            ]:
                bass_line.append((b0 + off, note, dur))
        elif style == "hold":
            bass_line.append((b0, root, 1.9))
            bass_line.append((b0 + 2, fifth, 1.8))

    roots = [
        ("A2", "E3", "pump"), ("A2", "E3", "gallop"), ("F2", "C3", "pump"), ("G2", "D3", "gallop"),
        ("A2", "E3", "pump"), ("C3", "G3", "pump"), ("F2", "C3", "gallop"), ("E2", "B2", "pump"),
        ("D2", "A2", "pump"), ("D2", "A2", "gallop"), ("A2", "E3", "pump"), ("G2", "D3", "hold"),
        ("A2", "E3", "gallop"), ("F2", "C3", "pump"), ("C3", "G3", "gallop"), ("G2", "D3", "pump"),
        ("F2", "C3", "hold"), ("E2", "B2", "pump"),
    ]
    for bar, (r, f, st) in enumerate(roots):
        add_bass_bar(bar, r, f, st)

    # Lead hook — staggered vs bass (off-beat / longer tones)
    lead_intro = [
        (0.0, None, 2.0),
        (2.0, "A4", 0.5), (2.5, "C5", 0.5), (3.0, "E5", 1.0),
        (4.0, "D5", 1.0), (5.0, "C5", 0.5), (5.5, "B4", 0.5), (6.0, "A4", 2.0),
        (8.0, "F4", 0.5), (8.5, "A4", 0.5), (9.0, "C5", 1.0), (10.0, "A4", 1.0),
        (11.0, "G4", 1.0),
        (12.0, "B4", 0.5), (12.5, "D5", 0.5), (13.0, "G5", 1.0), (14.0, "E5", 2.0),
    ]
    lead_hook = [
        (0.0, "A4", 0.5), (0.5, "C5", 0.5), (1.0, "E5", 0.5), (1.5, "A5", 0.5),
        (2.0, "G5", 1.0), (3.0, "E5", 1.0),
        (4.0, "C5", 0.5), (4.5, "D5", 0.5), (5.0, "E5", 1.0), (6.0, "G5", 0.5), (6.5, "E5", 0.5), (7.0, "C5", 1.0),
        (8.0, "F5", 1.0), (9.0, "C5", 0.5), (9.5, "A4", 0.5), (10.0, "F4", 1.0), (11.0, "A4", 1.0),
        (12.0, "G4", 0.5), (12.5, "B4", 0.5), (13.0, "E5", 1.0), (14.0, "D5", 1.0), (15.0, "B4", 1.0),
    ]
    lead_rise = [
        (0.0, "D5", 0.5), (0.5, "F5", 0.5), (1.0, "A5", 1.0), (2.0, "F5", 1.0), (3.0, "D5", 1.0),
        (4.0, "E5", 0.5), (4.5, "G5", 0.5), (5.0, "A5", 1.5), (6.5, "E5", 0.5), (7.0, "C5", 1.0),
        (8.0, "A4", 1.0), (9.0, "C5", 1.0), (10.0, "E5", 1.0), (11.0, "A5", 1.0),
        (12.0, "G5", 2.0), (14.0, "D5", 1.0), (15.0, "B4", 1.0),
    ]
    lead_hook2 = [
        (0.0, "E5", 0.5), (0.5, "C5", 0.5), (1.0, "A4", 0.5), (1.5, "C5", 0.5),
        (2.0, "E5", 1.0), (3.0, "A5", 1.0),
        (4.0, "F5", 0.75), (4.75, "E5", 0.25), (5.0, "D5", 1.0), (6.0, "C5", 1.0), (7.0, "A4", 1.0),
        (8.0, "G5", 0.5), (8.5, "E5", 0.5), (9.0, "C5", 1.0), (10.0, "G4", 1.0), (11.0, "E5", 1.0),
        (12.0, "D5", 1.5), (13.5, "B4", 0.5), (14.0, "G4", 2.0),
    ]
    lead_tag = [
        (0.0, "C5", 1.0), (1.0, "A4", 1.0), (2.0, "F4", 2.0),
        (4.0, "B4", 1.0), (5.0, "G4", 1.0), (6.0, "E4", 1.0), (7.0, "A4", 1.0),
    ]

    # Arp accompaniment — different rhythm from bass (16ths vs quarter pumps)
    chords_a = [["A3", "C4", "E4", "A4"], ["A3", "C4", "E4", "G4"], ["F3", "A3", "C4", "F4"], ["G3", "B3", "D4", "G4"]]
    chords_b = [["A3", "C4", "E4", "A4"], ["C4", "E4", "G4", "C5"], ["F3", "A3", "C4", "A4"], ["E3", "G3", "B3", "E4"]]
    chords_c = [["D3", "F3", "A3", "D4"], ["D3", "F3", "A3", "C4"], ["A3", "C4", "E4", "A4"], ["G3", "B3", "D4", "G4"]]
    pattern_main = [0, 1, 2, 1, 3, 2, 1, 0, 0, 2, 1, 3, 2, 1, 0, 1]
    pattern_alt = [0, -1, 2, -1, 1, 3, -1, 2, 0, 1, -1, 3, 2, -1, 1, 0]

    arp = []
    arp += arp_pattern(chords_a, 0, 4, bpm, pattern_main)
    arp += arp_pattern(chords_b, 16, 4, bpm, pattern_alt)
    arp += arp_pattern(chords_c, 32, 4, bpm, pattern_main)
    arp += arp_pattern(chords_b, 48, 4, bpm, pattern_alt)
    # lighter tag arp
    arp += arp_pattern([["F3", "A3", "C4", "F4"], ["E3", "G3", "B3", "E4"]], 64, 2, bpm, [0, 2, 1, 3, 0, 1, 2, 1] * 2)

    schedule_notes(buf, bass_line, bpm, "bass_sine", gain=0.38)
    schedule_notes(buf, arp, bpm, "lead_square", gain=0.10)
    schedule_notes(buf, melody_events(lead_intro, 0), bpm, "lead_pulse", gain=0.26)
    schedule_notes(buf, melody_events(lead_hook, 16), bpm, "lead_pulse", gain=0.30)
    schedule_notes(buf, melody_events(lead_rise, 32), bpm, "lead_pulse", gain=0.29)
    schedule_notes(buf, melody_events(lead_hook2, 48), bpm, "lead_pulse", gain=0.30)
    schedule_notes(buf, melody_events(lead_tag, 64), bpm, "lead_pulse", gain=0.27)

    # Soft pad swells on rise + tag for cohesion (third texture, still coordinated)
    pad_events = []
    for start, notes, dur in [
        (32, ["D3", "A3", "F4"], 8),
        (40, ["A3", "E4", "C5"], 8),
        (64, ["F3", "C4", "A4"], 4),
        (68, ["E3", "B3", "G4"], 4),
    ]:
        for note in notes:
            pad_events.append((float(start), note, float(dur)))
    schedule_notes(buf, pad_events, bpm, "pad", gain=0.09)

    return buf


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    out_dir = root / "Assets" / "Resources" / "Audio"

    menu = build_menu_track()
    level = build_level_track()

    # Enforce minimum length vs previous clips
    assert len(menu) / SR >= 8.96 * 3 - 0.01, len(menu) / SR
    assert len(level) / SR >= 7.04 * 3 - 0.01, len(level) / SR

    write_wav(out_dir / "Music_Menu.wav", menu)
    write_wav(out_dir / "Music_Level.wav", level)


if __name__ == "__main__":
    main()
