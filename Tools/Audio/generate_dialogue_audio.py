#!/usr/bin/env python3
"""Generate dialogue voice clips from the Unity dialogue manifest.

The manifest keeps stable Unity asset paths. Regenerating an existing clip at
the same path preserves the .meta GUID and the DialogueVoiceClipCatalog links.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any


API_BASE_URL = "https://api.elevenlabs.io/v1"
DEFAULT_MANIFEST = "Assets/Audio/Dialogue/voice-generation-manifest.mp3.json"
DEFAULT_MODEL = "eleven_multilingual_v2"
DEFAULT_OUTPUT_FORMAT = "mp3_44100_128"


@dataclass
class VoiceConfig:
    voice_id: str | None = None
    name: str | None = None
    model_id: str | None = None
    output_format: str | None = None
    voice_settings: dict[str, Any] | None = None


class ElevenLabsClient:
    def __init__(self, api_key: str, base_url: str = API_BASE_URL) -> None:
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")
        self._voices: list[dict[str, Any]] | None = None

    def list_voices(self) -> list[dict[str, Any]]:
        if self._voices is not None:
            return self._voices

        request = urllib.request.Request(
            f"{self.base_url}/voices",
            headers={"xi-api-key": self.api_key},
            method="GET",
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            payload = json.loads(response.read().decode("utf-8"))

        voices = payload.get("voices", [])
        if not isinstance(voices, list):
            raise RuntimeError("Unexpected ElevenLabs voices response.")

        self._voices = voices
        return voices

    def resolve_voice_name(self, name_or_id: str) -> str:
        normalized = name_or_id.casefold()
        for voice in self.list_voices():
            voice_id = str(voice.get("voice_id", ""))
            voice_name = str(voice.get("name", ""))
            if voice_id == name_or_id or voice_name.casefold() == normalized:
                return voice_id

        raise RuntimeError(f"Could not find an ElevenLabs voice named '{name_or_id}'.")

    def create_speech(
        self,
        *,
        voice_id: str,
        text: str,
        model_id: str,
        output_format: str,
        voice_settings: dict[str, Any] | None,
        seed: int | None,
    ) -> bytes:
        query = urllib.parse.urlencode({"output_format": output_format})
        url = f"{self.base_url}/text-to-speech/{urllib.parse.quote(voice_id)}?{query}"
        body: dict[str, Any] = {
            "text": text,
            "model_id": model_id,
        }
        if voice_settings:
            body["voice_settings"] = voice_settings
        if seed is not None:
            body["seed"] = seed

        data = json.dumps(body).encode("utf-8")
        request = urllib.request.Request(
            url,
            data=data,
            headers={
                "xi-api-key": self.api_key,
                "Content-Type": "application/json",
                "Accept": "audio/mpeg",
            },
            method="POST",
        )
        with urllib.request.urlopen(request, timeout=120) as response:
            return response.read()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate Unity dialogue clips with ElevenLabs text to speech."
    )
    parser.add_argument(
        "--manifest",
        default=DEFAULT_MANIFEST,
        help=f"Dialogue generation manifest. Default: {DEFAULT_MANIFEST}",
    )
    parser.add_argument(
        "--voice-map",
        help=(
            "Optional JSON map from manifest voice aliases to ElevenLabs voice IDs "
            "or objects with voice_id/name/model_id/output_format/voice_settings."
        ),
    )
    parser.add_argument(
        "--api-key-env",
        default="ELEVENLABS_API_KEY",
        help="Environment variable that contains the ElevenLabs API key.",
    )
    parser.add_argument("--model", default=DEFAULT_MODEL, help="Default ElevenLabs model ID.")
    parser.add_argument(
        "--output-format",
        default=DEFAULT_OUTPUT_FORMAT,
        help="Default ElevenLabs output format.",
    )
    parser.add_argument("--seed", type=int, help="Optional deterministic sampling seed.")
    parser.add_argument("--game", action="append", help="Only generate entries for this game.")
    parser.add_argument("--line-id", action="append", help="Only generate this line ID.")
    parser.add_argument("--voice", action="append", help="Only generate entries with this voice alias.")
    parser.add_argument(
        "--resolve-voice-names",
        action="store_true",
        help="Look up manifest voice values or voice-map names in the ElevenLabs voice list.",
    )
    parser.add_argument(
        "--print-voices",
        action="store_true",
        help="Print available ElevenLabs voices and exit.",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing audio files. Existing files are skipped by default.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print what would be generated without calling text-to-speech.",
    )
    parser.add_argument(
        "--update-manifest-bytes",
        action="store_true",
        help="After successful generation, update each generated entry's bytes field.",
    )
    parser.add_argument(
        "--sleep",
        type=float,
        default=0.0,
        help="Seconds to sleep between generation requests.",
    )
    return parser.parse_args()


def load_manifest(path: Path) -> list[dict[str, Any]]:
    with path.open("r", encoding="utf-8") as file:
        data = json.load(file)

    if not isinstance(data, list):
        raise ValueError(f"Manifest must contain a JSON list: {path}")

    return data


def load_voice_map(path: str | None) -> dict[str, VoiceConfig]:
    if not path:
        return {}

    with Path(path).open("r", encoding="utf-8") as file:
        raw_map = json.load(file)

    if not isinstance(raw_map, dict):
        raise ValueError("Voice map must be a JSON object.")

    voice_map: dict[str, VoiceConfig] = {}
    for alias, raw_config in raw_map.items():
        if isinstance(raw_config, str):
            voice_map[alias] = VoiceConfig(voice_id=raw_config)
            continue

        if not isinstance(raw_config, dict):
            raise ValueError(f"Voice map entry for '{alias}' must be a string or object.")

        settings = raw_config.get("voice_settings", raw_config.get("settings"))
        if settings is not None and not isinstance(settings, dict):
            raise ValueError(f"voice_settings for '{alias}' must be an object.")

        voice_map[alias] = VoiceConfig(
            voice_id=raw_config.get("voice_id"),
            name=raw_config.get("name"),
            model_id=raw_config.get("model_id"),
            output_format=raw_config.get("output_format"),
            voice_settings=settings,
        )

    return voice_map


def filter_entries(entries: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    games = set(args.game or [])
    line_ids = set(args.line_id or [])
    voices = set(args.voice or [])

    selected: list[dict[str, Any]] = []
    for entry in entries:
        if games and entry.get("game") not in games:
            continue
        if line_ids and entry.get("lineId") not in line_ids:
            continue
        if voices and entry.get("voice") not in voices:
            continue
        selected.append(entry)

    return selected


def require_text(entry: dict[str, Any], key: str) -> str:
    value = entry.get(key)
    if not isinstance(value, str) or not value.strip():
        line_id = entry.get("lineId", "<unknown>")
        raise ValueError(f"Manifest entry {line_id} is missing string field '{key}'.")
    return value


def resolve_voice_id(
    *,
    alias: str,
    voice_map: dict[str, VoiceConfig],
    client: ElevenLabsClient | None,
    resolve_names: bool,
) -> tuple[str, VoiceConfig]:
    config = voice_map.get(alias, VoiceConfig(voice_id=alias))
    if config.voice_id:
        return config.voice_id, config

    name = config.name or alias
    if resolve_names:
        if client is None:
            raise RuntimeError("Voice name resolution requires an ElevenLabs API key.")
        return client.resolve_voice_name(name), config

    return name, config


def print_voices(client: ElevenLabsClient) -> None:
    for voice in client.list_voices():
        voice_id = voice.get("voice_id", "")
        name = voice.get("name", "")
        category = voice.get("category", "")
        print(f"{voice_id}\t{name}\t{category}")


def write_audio(path: Path, audio: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = path.with_suffix(path.suffix + ".tmp")
    temp_path.write_bytes(audio)
    temp_path.replace(path)


def write_manifest(path: Path, entries: list[dict[str, Any]]) -> None:
    path.write_text(json.dumps(entries, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    project_root = Path.cwd()
    manifest_path = (project_root / args.manifest).resolve()
    entries = load_manifest(manifest_path)
    selected = filter_entries(entries, args)
    voice_map = load_voice_map(args.voice_map)

    api_key = os.environ.get(args.api_key_env)
    client = ElevenLabsClient(api_key) if api_key else None

    if args.print_voices:
        if client is None:
            print(f"Missing API key in {args.api_key_env}.", file=sys.stderr)
            return 2
        try:
            print_voices(client)
        except urllib.error.HTTPError as error:
            body = error.read().decode("utf-8", errors="replace")
            print(f"Could not list ElevenLabs voices: HTTP {error.code} {body}", file=sys.stderr)
            return 1
        return 0

    if not selected:
        print("No manifest entries matched the filters.")
        return 0

    generated = 0
    skipped = 0
    planned = 0
    updated_bytes = False

    for entry in selected:
        line_id = require_text(entry, "lineId")
        text = require_text(entry, "text")
        alias = require_text(entry, "voice")
        relative_output_path = require_text(entry, "path")
        output_path = project_root / relative_output_path

        if output_path.exists() and not args.force:
            print(f"skip existing {line_id}: {relative_output_path}")
            skipped += 1
            continue

        if client is None and not args.dry_run:
            print(f"Missing API key in {args.api_key_env}.", file=sys.stderr)
            return 2

        voice_id, config = resolve_voice_id(
            alias=alias,
            voice_map=voice_map,
            client=client,
            resolve_names=args.resolve_voice_names,
        )
        model_id = config.model_id or args.model
        output_format = config.output_format or args.output_format

        print(f"generate {line_id}: voice={alias} -> {voice_id}, path={relative_output_path}")
        planned += 1

        if args.dry_run:
            continue

        assert client is not None
        try:
            audio = client.create_speech(
                voice_id=voice_id,
                text=text,
                model_id=model_id,
                output_format=output_format,
                voice_settings=config.voice_settings,
                seed=args.seed,
            )
        except urllib.error.HTTPError as error:
            body = error.read().decode("utf-8", errors="replace")
            print(f"ElevenLabs request failed for {line_id}: HTTP {error.code} {body}", file=sys.stderr)
            return 1

        write_audio(output_path, audio)
        entry["bytes"] = len(audio)
        generated += 1
        updated_bytes = True

        if args.sleep > 0:
            time.sleep(args.sleep)

    if args.update_manifest_bytes and updated_bytes:
        write_manifest(manifest_path, entries)

    print(f"done: planned={planned}, generated={generated}, skipped={skipped}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
