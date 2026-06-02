#!/usr/bin/env python3
"""Professional procedural audio generator for BlokDunyasi.

Outputs:
- SFX as WAV (same filenames used by Unity bank)
- Music as OGG (same filenames used by Unity bank)
"""

from __future__ import annotations

import math
import os
from dataclasses import dataclass

import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, lfilter

SAMPLE_RATE = 44100
OUTPUT_DIR = "Assets/Audio"
RNG = np.random.default_rng(24032026)


def db_to_amp(db: float) -> float:
    return float(10 ** (db / 20.0))


def time_axis(duration: float) -> np.ndarray:
    sample_count = max(1, int(duration * SAMPLE_RATE))
    return np.arange(sample_count, dtype=np.float32) / SAMPLE_RATE


def adsr(duration: float, attack: float, decay: float, sustain_level: float, release: float) -> np.ndarray:
    sample_count = max(1, int(duration * SAMPLE_RATE))
    envelope = np.zeros(sample_count, dtype=np.float32)

    a = int(attack * SAMPLE_RATE)
    d = int(decay * SAMPLE_RATE)
    r = int(release * SAMPLE_RATE)
    s = max(0, sample_count - a - d - r)

    cursor = 0
    if a > 0:
        envelope[cursor:cursor + a] = np.linspace(0, 1, a, endpoint=False, dtype=np.float32)
        cursor += a
    if d > 0 and cursor < sample_count:
        decay_end = min(sample_count, cursor + d)
        envelope[cursor:decay_end] = np.linspace(1, sustain_level, decay_end - cursor, endpoint=False, dtype=np.float32)
        cursor = decay_end
    if s > 0 and cursor < sample_count:
        sustain_end = min(sample_count, cursor + s)
        envelope[cursor:sustain_end] = sustain_level
        cursor = sustain_end
    if cursor < sample_count:
        remain = sample_count - cursor
        start_level = envelope[cursor - 1] if cursor > 0 else sustain_level
        envelope[cursor:] = np.linspace(start_level, 0, remain, endpoint=True, dtype=np.float32)

    return envelope


def lowpass(signal: np.ndarray, cutoff_hz: float, order: int = 2) -> np.ndarray:
    b, a = butter(order, cutoff_hz / (SAMPLE_RATE * 0.5), btype="low")
    return lfilter(b, a, signal).astype(np.float32)


def highpass(signal: np.ndarray, cutoff_hz: float, order: int = 2) -> np.ndarray:
    b, a = butter(order, cutoff_hz / (SAMPLE_RATE * 0.5), btype="high")
    return lfilter(b, a, signal).astype(np.float32)


def soft_limiter(signal: np.ndarray, drive: float = 1.8) -> np.ndarray:
    return np.tanh(signal * drive) / np.tanh(drive)


def normalize_peak(signal: np.ndarray, target_db: float = -1.2) -> np.ndarray:
    peak = float(np.max(np.abs(signal)))
    if peak < 1e-8:
        return signal
    return signal * (db_to_amp(target_db) / peak)


def loop_seam(signal: np.ndarray, crossfade_ms: float = 30) -> np.ndarray:
    fade_len = int((crossfade_ms / 1000.0) * SAMPLE_RATE)
    if fade_len * 2 >= len(signal):
        return signal
    fade_in = np.linspace(0, 1, fade_len, dtype=np.float32)
    fade_out = 1 - fade_in
    head = signal[:fade_len].copy()
    tail = signal[-fade_len:].copy()
    signal[:fade_len] = head * fade_in + tail * fade_out
    signal[-fade_len:] = signal[:fade_len]
    return signal


def mono_reverb(signal: np.ndarray, wet: float = 0.2) -> np.ndarray:
    wet_signal = signal.astype(np.float32).copy()
    taps = [0.021, 0.033, 0.047, 0.061, 0.079]
    gains = [0.22, 0.18, 0.14, 0.10, 0.07]
    for tap, gain in zip(taps, gains):
        delay = int(tap * SAMPLE_RATE)
        if delay <= 0 or delay >= len(signal):
            continue
        wet_signal[delay:] += signal[:-delay] * gain

    return signal * (1.0 - wet) + wet_signal * wet


def sine(freq: float | np.ndarray, duration: float, phase: float = 0.0) -> np.ndarray:
    t = time_axis(duration)
    if isinstance(freq, np.ndarray):
        omega = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
        return np.sin(omega + phase).astype(np.float32)
    return np.sin(2 * np.pi * freq * t + phase).astype(np.float32)


def triangle(freq: float, duration: float) -> np.ndarray:
    t = time_axis(duration)
    return (2 * np.abs(2 * ((freq * t) % 1.0) - 1) - 1).astype(np.float32)


def noise(duration: float) -> np.ndarray:
    return RNG.standard_normal(int(duration * SAMPLE_RATE)).astype(np.float32)


def midi_to_hz(midi: int) -> float:
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


@dataclass
class DrumKit:
    kick: np.ndarray
    snare: np.ndarray
    hat: np.ndarray


def make_kick(duration: float = 0.22) -> np.ndarray:
    t = time_axis(duration)
    sweep = np.linspace(150, 45, len(t), dtype=np.float32)
    body = sine(sweep, duration)
    click = highpass(noise(duration), 3000) * np.exp(-t * 70)
    env = adsr(duration, 0.001, 0.05, 0.0, 0.12)
    signal = (body * 0.9 + click * 0.12) * env
    return normalize_peak(soft_limiter(signal), -4.5)


def make_snare(duration: float = 0.19) -> np.ndarray:
    t = time_axis(duration)
    tone = sine(195, duration) * np.exp(-t * 16)
    air = highpass(noise(duration), 1600) * np.exp(-t * 11)
    env = adsr(duration, 0.001, 0.04, 0.0, 0.11)
    signal = (tone * 0.25 + air * 0.95) * env
    return normalize_peak(signal, -7.0)


def make_hat(duration: float = 0.07) -> np.ndarray:
    t = time_axis(duration)
    air = highpass(noise(duration), 7000)
    env = np.exp(-t * 65)
    signal = air * env
    return normalize_peak(signal, -12.0)


def make_drum_kit() -> DrumKit:
    return DrumKit(kick=make_kick(), snare=make_snare(), hat=make_hat())


def place(sound: np.ndarray, target: np.ndarray, start_sample: int, gain: float = 1.0) -> None:
    if start_sample >= len(target):
        return
    end_sample = min(len(target), start_sample + len(sound))
    if end_sample <= start_sample:
        return
    target[start_sample:end_sample] += sound[: end_sample - start_sample] * gain


def fm_pluck(freq: float, duration: float, mod_ratio: float = 2.0, index: float = 3.0) -> np.ndarray:
    t = time_axis(duration)
    mod = np.sin(2 * np.pi * freq * mod_ratio * t) * (index * np.exp(-t * 8.0))
    car = np.sin(2 * np.pi * freq * t + mod)
    env = adsr(duration, 0.003, 0.07, 0.0, 0.15)
    return (car * env).astype(np.float32)


def supersaw(freq: float, duration: float) -> np.ndarray:
    t = time_axis(duration)
    detunes = [-0.012, 0.0, 0.013]
    phases = RNG.uniform(0, np.pi * 2, len(detunes))
    layers = np.zeros_like(t)
    for detune, phase in zip(detunes, phases):
        f = freq * (1 + detune)
        saw = 2 * ((f * t + phase / (2 * np.pi)) % 1.0) - 1
        layers += saw
    return (layers / len(detunes)).astype(np.float32)


def build_ui_click() -> np.ndarray:
    duration = 0.11
    t = time_axis(duration)
    transient = highpass(noise(duration), 2500) * np.exp(-t * 95)
    tone = sine(1700, duration) * adsr(duration, 0.001, 0.015, 0.0, 0.06)
    body = sine(2300, duration) * adsr(duration, 0.001, 0.01, 0.0, 0.04) * 0.5
    signal = transient * 0.35 + tone * 0.9 + body
    signal = soft_limiter(signal)
    return normalize_peak(signal, -4.5)


def build_block_place() -> np.ndarray:
    duration = 0.21
    t = time_axis(duration)
    low = sine(np.linspace(220, 75, len(t), dtype=np.float32), duration) * np.exp(-t * 15)
    mid = triangle(320, duration) * np.exp(-t * 24)
    hit = lowpass(noise(duration), 1800) * np.exp(-t * 40)
    signal = low * 0.85 + mid * 0.25 + hit * 0.15
    signal *= adsr(duration, 0.001, 0.03, 0.0, 0.15)
    return normalize_peak(soft_limiter(signal), -3.8)


def build_invalid_drop() -> np.ndarray:
    duration = 0.19
    t = time_axis(duration)
    down = np.linspace(650, 180, len(t), dtype=np.float32)
    growl = sine(down, duration)
    buzz = triangle(95, duration) * 0.5
    noise_part = highpass(noise(duration), 1200) * np.exp(-t * 20)
    signal = (growl * 0.7 + buzz * 0.4) * np.exp(-t * 8) + noise_part * 0.2
    signal *= adsr(duration, 0.001, 0.03, 0.0, 0.13)
    return normalize_peak(signal, -5.5)


def build_line_clear() -> np.ndarray:
    duration = 0.42
    notes = [76, 79, 83, 88]
    note_dur = duration / len(notes)
    signal = np.zeros(int(duration * SAMPLE_RATE), dtype=np.float32)
    cursor = 0
    for midi in notes:
        tone = fm_pluck(midi_to_hz(midi), note_dur, mod_ratio=1.5, index=4.2)
        sparkle = highpass(noise(note_dur), 4500) * adsr(note_dur, 0.002, 0.03, 0.0, 0.08) * 0.08
        seg = (tone + sparkle) * 0.9
        place(seg, signal, cursor)
        cursor += int(note_dur * SAMPLE_RATE)
    signal = mono_reverb(signal, wet=0.16)
    return normalize_peak(signal, -3.4)


def build_combo() -> np.ndarray:
    duration = 0.33
    t = time_axis(duration)
    rise = np.linspace(480, 1320, len(t), dtype=np.float32)
    lead = sine(rise, duration)
    harm = sine(rise * 1.99, duration) * 0.3
    noise_sheen = highpass(noise(duration), 5000) * np.exp(-t * 14) * 0.08
    env = adsr(duration, 0.003, 0.05, 0.0, 0.20)
    signal = (lead + harm) * env + noise_sheen
    signal = mono_reverb(signal, wet=0.12)
    return normalize_peak(signal, -3.1)


def build_game_over_sfx() -> np.ndarray:
    duration = 0.95
    melody = [72, 68, 64, 59]
    note_dur = 0.21
    signal = np.zeros(int(duration * SAMPLE_RATE), dtype=np.float32)
    cursor = 0
    for midi in melody:
        tone = supersaw(midi_to_hz(midi), note_dur)
        env = adsr(note_dur, 0.01, 0.07, 0.0, 0.12)
        place(tone * env * 0.4, signal, cursor)
        cursor += int(note_dur * SAMPLE_RATE)
    rumble = lowpass(noise(duration), 180) * np.exp(-time_axis(duration) * 4) * 0.07
    signal += rumble
    signal = mono_reverb(signal, wet=0.18)
    return normalize_peak(signal, -4.0)


def sidechain_duck(track: np.ndarray, trigger: np.ndarray, amount: float = 0.35) -> np.ndarray:
    env = np.clip(np.abs(trigger) * 2.8, 0, 1)
    duck = 1.0 - env * amount
    return track * duck


def build_menu_music() -> np.ndarray:
    bpm = 92
    beats = 24
    total_seconds = beats * (60.0 / bpm)
    total_samples = int(total_seconds * SAMPLE_RATE)
    track = np.zeros(total_samples, dtype=np.float32)

    chord_prog = [
        [57, 60, 64, 67],
        [55, 59, 62, 65],
        [53, 57, 60, 64],
        [55, 59, 62, 67],
    ]

    bar_samples = int(4 * (60.0 / bpm) * SAMPLE_RATE)
    for bar in range(6):
        chord = chord_prog[bar % len(chord_prog)]
        chord_audio = np.zeros(bar_samples, dtype=np.float32)
        duration = bar_samples / SAMPLE_RATE
        for note in chord:
            rhodes = fm_pluck(midi_to_hz(note), duration, mod_ratio=1.0, index=2.2)
            pad = supersaw(midi_to_hz(note - 12), duration) * adsr(duration, 0.25, 0.45, 0.7, 0.4)
            chord_audio += rhodes * 0.20 + lowpass(pad, 1600) * 0.05
        place(chord_audio, track, bar * bar_samples)

    bass_step = int((60.0 / bpm) * SAMPLE_RATE)
    bass_notes = [33, 33, 31, 31, 29, 29, 31, 31]
    for i in range(beats):
        note = bass_notes[i % len(bass_notes)]
        dur = 0.42
        bass = sine(midi_to_hz(note), dur) * adsr(dur, 0.01, 0.12, 0.15, 0.18)
        bass = lowpass(bass, 180)
        place(bass * 0.55, track, i * bass_step)

    hats = np.zeros_like(track)
    hat_step = int(0.5 * (60.0 / bpm) * SAMPLE_RATE)
    hat_sound = make_hat(0.05) * 0.8
    for i in range((len(track) // hat_step) - 1):
        place(hat_sound, hats, i * hat_step)

    track += hats
    track = lowpass(track, 11500)
    track = mono_reverb(track, wet=0.20)
    track = loop_seam(track, 45)
    track = normalize_peak(soft_limiter(track, 1.35), -1.4)
    return track


def build_gameplay_music() -> np.ndarray:
    bpm = 122
    beats = 32
    total_seconds = beats * (60.0 / bpm)
    total_samples = int(total_seconds * SAMPLE_RATE)

    drums = np.zeros(total_samples, dtype=np.float32)
    synth_bus = np.zeros(total_samples, dtype=np.float32)
    kit = make_drum_kit()

    beat_samples = int((60.0 / bpm) * SAMPLE_RATE)
    half_beat = beat_samples // 2

    for beat in range(beats):
        pos = beat * beat_samples
        if beat % 4 in (0, 2):
            place(kit.kick, drums, pos, 0.95)
        if beat % 4 in (1, 3):
            place(kit.snare, drums, pos, 0.8)
        place(kit.hat, drums, pos, 0.55)
        place(kit.hat * 0.8, drums, pos + half_beat, 0.45)

    bass_pattern = [33, 33, 36, 38, 33, 31, 29, 31]
    for beat in range(beats):
        note = bass_pattern[beat % len(bass_pattern)]
        dur = 0.38
        bass = triangle(midi_to_hz(note), dur) * adsr(dur, 0.005, 0.06, 0.18, 0.13)
        bass = lowpass(bass, 260)
        place(bass * 0.52, synth_bus, beat * beat_samples)

    arp_pattern = [69, 72, 76, 79, 76, 72]
    step = int(0.5 * beat_samples)
    arp_duration = 0.22
    for i in range((total_samples // step) - 1):
        note = arp_pattern[i % len(arp_pattern)] + (12 if (i // 12) % 2 == 1 else 0)
        pluck = fm_pluck(midi_to_hz(note), arp_duration, mod_ratio=2.2, index=4.0)
        place(pluck * 0.27, synth_bus, i * step)

    pads = np.zeros_like(synth_bus)
    bar_samples = beat_samples * 4
    pad_chords = [[57, 60, 64], [55, 59, 62], [53, 57, 60], [50, 53, 57]]
    for bar in range(math.ceil(total_samples / bar_samples)):
        chord = pad_chords[bar % len(pad_chords)]
        dur = (bar_samples / SAMPLE_RATE)
        part = np.zeros(bar_samples, dtype=np.float32)
        for note in chord:
            part += supersaw(midi_to_hz(note), dur) * adsr(dur, 0.2, 0.5, 0.7, 0.4) * 0.08
        place(lowpass(part, 2500), pads, bar * bar_samples)

    synth_bus += pads
    synth_bus = sidechain_duck(synth_bus, drums, amount=0.33)

    track = drums + synth_bus
    track = mono_reverb(track, wet=0.11)
    track = loop_seam(track, 35)
    track = normalize_peak(soft_limiter(track, 1.45), -1.0)
    return track


def build_gameover_music() -> np.ndarray:
    bpm = 70
    beats = 12
    total_seconds = beats * (60.0 / bpm)
    total_samples = int(total_seconds * SAMPLE_RATE)
    track = np.zeros(total_samples, dtype=np.float32)

    beat_samples = int((60.0 / bpm) * SAMPLE_RATE)
    chord_prog = [[45, 52, 57], [43, 50, 55], [41, 48, 53], [40, 47, 52]]

    for bar in range(4):
        chord = chord_prog[bar]
        dur = 4 * (60.0 / bpm)
        part = np.zeros(int(dur * SAMPLE_RATE), dtype=np.float32)
        for note in chord:
            part += lowpass(supersaw(midi_to_hz(note), dur), 1400) * adsr(dur, 0.2, 0.7, 0.5, 0.8) * 0.12
        place(part, track, bar * 4 * beat_samples)

    melody = [72, 69, 67, 64, 62]
    step = int(2 * beat_samples)
    note_duration = 1.1
    for i, midi in enumerate(melody):
        note = sine(midi_to_hz(midi), note_duration) + sine(midi_to_hz(midi) * 2, note_duration) * 0.2
        env = adsr(note_duration, 0.02, 0.2, 0.28, 0.5)
        place(note * env * 0.20, track, i * step)

    rumble = lowpass(noise(total_seconds), 120) * np.linspace(0.2, 0.0, total_samples, dtype=np.float32) * 0.05
    track += rumble
    track = mono_reverb(track, wet=0.24)
    track = normalize_peak(soft_limiter(track, 1.25), -1.5)
    return track


def write_wav(path: str, signal: np.ndarray) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    int16 = (np.clip(signal, -1.0, 1.0) * 32767).astype(np.int16)
    wavfile.write(path, SAMPLE_RATE, int16)
    print(f"✓ WAV  {path}")


def main() -> None:
    print("🎧 Producing professional audio set...\n")

    sfx_dir = os.path.join(OUTPUT_DIR, "SFX")
    music_dir = os.path.join(OUTPUT_DIR, "Music")

    print("[SFX] Rendering...")
    write_wav(os.path.join(sfx_dir, "ui_click.wav"), build_ui_click())
    write_wav(os.path.join(sfx_dir, "block_place.wav"), build_block_place())
    write_wav(os.path.join(sfx_dir, "invalid_drop.wav"), build_invalid_drop())
    write_wav(os.path.join(sfx_dir, "line_clear.wav"), build_line_clear())
    write_wav(os.path.join(sfx_dir, "combo.wav"), build_combo())
    write_wav(os.path.join(sfx_dir, "game_over.wav"), build_game_over_sfx())

    print("[MUSIC] Rendering menu...")
    write_wav(os.path.join(music_dir, "menu_music.wav"), build_menu_music())
    print("[MUSIC] Rendering gameplay...")
    write_wav(os.path.join(music_dir, "gameplay_music.wav"), build_gameplay_music())
    print("[MUSIC] Rendering gameover...")
    write_wav(os.path.join(music_dir, "gameover_music.wav"), build_gameover_music())

    print("\n✨ Done. New audio files are ready for Unity reimport.")


if __name__ == "__main__":
    main()
