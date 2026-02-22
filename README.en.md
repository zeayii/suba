# suba — Subtitle Generation CLI

English | [简体中文](./README.md)

**Organization:** zeayii  
**Project:** suba  
**CLI executable:** `suba`

> This project transcribes and translates media into subtitle files, with batch processing, stage-level scheduling policies, GPU/CPU execution control, and cross-platform release artifacts.

---

## Table of Contents

- [Overview](#overview)
- [Pipeline](#pipeline)
- [Input File (TOML)](#input-file-toml)
- [CLI Usage](#cli-usage)
- [Options (Grouped by Stage)](#options-grouped-by-stage)
- [Output Artifacts and Naming](#output-artifacts-and-naming)
- [Release Packages and Platform Matrix](#release-packages-and-platform-matrix)
- [Model Directory Layout and Download Links](#model-directory-layout-and-download-links)
- [Minimal GPU Runtime List and Layout](#minimal-gpu-runtime-list-and-layout)
- [Build and Test](#build-and-test)
- [Troubleshooting](#troubleshooting)

---

## Overview

Suba provides:

- media input to source-language and translated subtitle output;
- stage-aware scheduling for transcription and translation;
- translation providers via `Ollama` and `OpenAI`;
- user-friendly `TOML` with multiline prompts, avoiding JSON escaping;
- cross-platform `CPU` packages and `Windows/Linux` `GPU` packages.

Boundary:

- **Suba (this repo):** audio prep, VAD, overlap handling, separation, transcription, translation, subtitle writing.
- **External dependencies:** FFmpeg, ONNX Runtime, translation providers (Ollama/OpenAI).

---

## Pipeline

Default stage flow:

1. `AudioPrepare`: extract/normalize audio when needed;
2. `VAD`: detect speech regions;
3. `OverlapResolve`: overlap detection and optional voice separation;
4. `Transcribe`: convert speech segments to source subtitles;
5. `Translate`: translate source subtitles to target language;
6. `SubtitleWrite`: persist source and translated subtitle files.

Translation execution modes:

- `PerTask`: translate each task right after it is transcribed.
- `BatchAfterTranscription`: transcribe all tasks first, then translate in batch.

---

## Input File (TOML)

Main argument is a TOML path (`arguments-toml-path`).  
TOML contains only three fields: `inputs`, `prompt`, and `fix_prompt`.

Template:

```toml
inputs = [
  "D:/Media/movie-001.mp4",
  "D:/Media/movie-002.mkv"
]

prompt = """
You are a professional subtitle translation assistant.
Output translated text only, without explanations or extra formatting.
"""

fix_prompt = """
You are a translation repair assistant.
Rewrite the given text into one natural and concise subtitle sentence.
"""
```

Generate template:

```bash
suba init
```

Generated file: `arguments.template.toml`

---

## CLI Usage

Basic run:

```bash
suba ./arguments.template.toml --ffmpeg-path "/usr/bin/ffmpeg" --models-root "./models"
```

Windows:

```powershell
suba.exe .\arguments.template.toml --ffmpeg-path "C:\Program Files\ffmpeg\bin\ffmpeg.exe" --models-root ".\models"
```

Provider examples:

```bash
# OpenAI
suba ./arguments.template.toml \
  --translation-provider OpenAi \
  --openai-api-key "YOUR_API_KEY" \
  --openai-model "gpt-4o-mini"

# Ollama
suba ./arguments.template.toml \
  --translation-provider Ollama \
  --ollama-base-url "http://localhost:11434" \
  --ollama-model "qwen3:14b"
```

---

## Options (Grouped by Stage)

For full list and defaults, use `suba --help`.

### 1) Global and Paths

- `arguments-toml-path`
- `--ffmpeg-path`
- `--models-root`
- `--cache-directory`
- `--log-directory`
- `--console-log-level`
- `--file-log-level`

### 2) Runtime Scheduling Policies

- `--max-degree-of-parallelism`
- `--command-timeout-seconds`
- `--translation-mode`
- `--gpu-conflict-policy`
- `--artifact-overwrite-policy`

### 3) Stage Device and Parallelism

- `--preprocess-device` / `--preprocess-parallelism`
- `--transcribe-device` / `--transcribe-parallelism`
- `--translate-device` / `--translate-parallelism`

Note:

- when provider is `OpenAi`, translation is remote API based, so local translation GPU execution is not used.

### 4) VAD and Overlap

- `--vad-threshold`
- `--vad-min-silence-ms`
- `--vad-min-speech-ms`
- `--vad-max-speech-seconds`
- `--vad-speech-pad-ms`
- `--overlap-detection-policy`
- `--overlap-onset` / `--overlap-offset`
- `--overlap-min-duration-on-seconds`
- `--overlap-min-duration-off-seconds`
- `--separated-vad-*`
- `--sepformer-normalize-output`

### 5) Transcription

- `--transcribe-language-policy`
- `--transcribe-language`
- `--no-speech-threshold`
- `--transcribe-max-new-tokens`
- `--transcribe-temperature`
- `--transcribe-beam-size`
- `--transcribe-best-of`
- `--transcribe-length-penalty`
- `--transcribe-repetition-penalty`
- `--transcribe-suppress-blank`
- `--transcribe-suppress-tokens`
- `--transcribe-without-timestamps`

### 6) Translation

- `--translation-provider`
- `--translate-language`
- `--translate-response-mode`
- `--translate-context-queue-size`
- `--translate-context-gap-ms`
- `--translate-partial-write-interval`

Ollama:

- `--ollama-base-url`
- `--ollama-model`

OpenAI:

- `--openai-base-url`
- `--openai-api-key`
- `--openai-model`

### 7) Subtitle Output

- `--subtitle-format-policy` (default `Vtt`)

---

## Output Artifacts and Naming

Subtitle naming:

```text
{source_file_name_without_ext}.{language_tag}.{subtitle_ext}
```

Examples:

- `movie.ja.vtt`
- `movie.zh-CN.vtt`

Partial translation checkpoints are written with `.partial` and replaced by final artifacts when completed.

---

## Release Packages and Platform Matrix

Workflow: `.github/workflows/publish-suba.yml`

CPU:

- `win-x64`
- `linux-x64`
- `osx-x64`

GPU:

- `win-x64` (with minimal CUDA/cuDNN runtime set)
- `linux-x64` (with minimal CUDA/cuDNN runtime set)

Note:

- `osx-x64` has no CUDA/cuDNN GPU package (modern CUDA runtime is not available on macOS).

---

## Model Directory Layout and Download Links

Under `--models-root`, the following layout is required (aligned with runtime validation):

```text
models/
  onnx-community/
    pyannote-segmentation-3.0/
      onnx/
        model.onnx
    kotoba-whisper-v2.2-ONNX/
      onnx/
        encoder_model.onnx
        encoder_model.onnx_data
        decoder_model.onnx
        decoder_with_past_model.onnx
      tokenizer.json
      added_tokens.json
      generation_config.json
  speechbrain/
    sepformer-wsj02mix/
      onnx/
        model.onnx
```

Recommended sources:

- `pyannote-segmentation-3.0` (ONNX):  
  https://huggingface.co/onnx-community/pyannote-segmentation-3.0
- `kotoba-whisper-v2.2-ONNX`:  
  https://huggingface.co/onnx-community/kotoba-whisper-v2.2-ONNX
- `sepformer-wsj02mix`: this project uses a self-exported ONNX artifact (`speechbrain/sepformer-wsj02mix/onnx/model.onnx`).  
  This exact packaged path is not published as an official one-click model bundle, so use the model shipped with this project release or export it with the project workflow and place it at the required path.

Notes:

- Directory levels and file names must match exactly, otherwise startup validation will fail.
- Model upgrades can be done by replacing files at the same paths; no CLI option changes are required.

---

## Minimal GPU Runtime List and Layout

If downloading the GPU package is slow in your environment, you can start from the `CPU` package and manually add CUDA/cuDNN runtime files.

Recommended extracted layout:

```text
suba/
  suba(.exe)
  arguments.template.toml
  README.md
  README.en.md
  onnxruntime* / libonnxruntime*
  <CUDA/cuDNN runtime files>
```

Windows minimal runtime files:

- `cudart64_12.dll`
- `cublas64_12.dll`
- `cublasLt64_12.dll`
- `cufft64_11.dll`
- `cudnn64_9.dll`
- `cudnn_ops64_9.dll`
- `cudnn_graph64_9.dll`
- `cudnn_adv64_9.dll`
- `cudnn_cnn64_9.dll`
- `cudnn_heuristic64_9.dll`
- `cudnn_engines_runtime_compiled64_9.dll`
- `cudnn_engines_precompiled64_9.dll`

Linux minimal runtime files:

- `libcudart.so.12`
- `libcublas.so.12`
- `libcublasLt.so.12`
- `libcufft.so.11`
- `libcudnn.so.9`
- `libcudnn_ops.so.9`
- `libcudnn_graph.so.9`
- `libcudnn_adv.so.9`
- `libcudnn_cnn.so.9`
- `libcudnn_heuristic.so.9`
- `libcudnn_engines_runtime_compiled.so.9`
- `libcudnn_engines_precompiled.so.9`

Notes:

- Place all files above in the same directory as the `suba` executable.
- Current runtime baseline is CUDA 12 + cuDNN 9.
- If you only use CPU inference, the `CPU` package is enough.

Download entry points and package mapping (for manual runtime assembly):

- CUDA redist root:  
  https://developer.download.nvidia.com/compute/cuda/redist/
- cuDNN redist root:  
  https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/

Windows:

- `cudart64_12.dll`: from `cuda_cudart/windows-x86_64/`, archive `cuda_cudart-windows-x86_64-*.zip`
- `cublas64_12.dll`, `cublasLt64_12.dll`: from `libcublas/windows-x86_64/`, archive `libcublas-windows-x86_64-*.zip`
- `cufft64_11.dll`: from `libcufft/windows-x86_64/`, archive `libcufft-windows-x86_64-*.zip`
- `cudnn*.dll`: from `https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/windows-x86_64/`, archive `cudnn-windows-x86_64-*.zip`

Linux:

- `libcudart.so.12`: from `cuda_cudart/linux-x86_64/`, archive `cuda_cudart-linux-x86_64-*.tar.xz`
- `libcublas.so.12`, `libcublasLt.so.12`: from `libcublas/linux-x86_64/`, archive `libcublas-linux-x86_64-*.tar.xz`
- `libcufft.so.11`: from `libcufft/linux-x86_64/`, archive `libcufft-linux-x86_64-*.tar.xz`
- `libcudnn*.so.9`: from `https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/linux-x86_64/`, archive `cudnn-linux-x86_64-*.tar.xz`

---

## Build and Test

```bash
dotnet build Suba.sln -v minimal
dotnet test Suba.sln -v minimal
```

---

## Troubleshooting

1. ONNX warning: `Some nodes were not assigned to the preferred execution providers`  
Usually harmless; ONNX logging is already reduced to Error level in this project.

2. GPU provider load failure due to missing runtime libs  
Use the corresponding `gpu` release package or ensure required CUDA/cuDNN libs are colocated with the executable.

3. Translation becomes very slow  
Review `prompt` and `fix_prompt` length and constraints. Overly complex prompts can heavily increase latency.
