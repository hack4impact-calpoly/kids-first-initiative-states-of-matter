# Dialogue Audio Generation

Use `generate_dialogue_audio.py` to regenerate dialogue clips from
`Assets/Audio/Dialogue/voice-generation-manifest.mp3.json`.

The manifest stores the dialogue text, voice alias, and Unity asset path for
each clip. Existing MP3s are skipped by default so a test run does not spend
ElevenLabs credits or overwrite Unity assets.

## Setup

Set an ElevenLabs API key in the shell:

```sh
export ELEVENLABS_API_KEY="..."
```

Create a local voice map from the example:

```sh
cp Tools/Audio/voice-map.elevenlabs.example.json Tools/Audio/voice-map.elevenlabs.local.json
```

Fill each alias with the ElevenLabs voice ID you want to use. The aliases in
the current manifest are `gary`, `lucy`, `narrator`, `patrice`, and `sam`.

## Useful Commands

List voices available to the API key:

```sh
python3 Tools/Audio/generate_dialogue_audio.py --print-voices
```

Dry-run one line without calling text-to-speech:

```sh
python3 Tools/Audio/generate_dialogue_audio.py \
  --dry-run \
  --force \
  --line-id kitchen.solid.intro.1 \
  --voice-map Tools/Audio/voice-map.elevenlabs.local.json
```

Regenerate one line in place:

```sh
python3 Tools/Audio/generate_dialogue_audio.py \
  --force \
  --line-id kitchen.solid.intro.1 \
  --voice-map Tools/Audio/voice-map.elevenlabs.local.json
```

Regenerate all kitchen lines:

```sh
python3 Tools/Audio/generate_dialogue_audio.py \
  --force \
  --game kitchen \
  --voice-map Tools/Audio/voice-map.elevenlabs.local.json \
  --sleep 0.5
```

Regenerating at the same `Assets/Audio/Dialogue/...mp3` paths keeps the Unity
`.meta` GUIDs, so `DialogueVoiceClipCatalog` references stay intact.
