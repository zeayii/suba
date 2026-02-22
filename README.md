# suba — 字幕生成命令行工具

[English](./README.en.md) | 简体中文

**组织（Organization）：** zeayii  
**工程（Project）：** suba  
**可执行名（CLI）：** `suba`

> 本项目用于将媒体文件转写并翻译为字幕，支持批处理、阶段化执行策略、GPU/CPU 组合调度和跨平台发布。

---

## 目录

- [概述](#概述)
- [处理流程](#处理流程)
- [输入文件（TOML）](#输入文件toml)
- [命令行用法](#命令行用法)
- [参数说明（按阶段）](#参数说明按阶段)
- [输出产物与命名规则](#输出产物与命名规则)
- [发布包与平台矩阵](#发布包与平台矩阵)
- [模型目录结构与下载坐标](#模型目录结构与下载坐标)
- [GPU 依赖最小清单与目录结构](#gpu-依赖最小清单与目录结构)
- [构建与测试](#构建与测试)
- [排错建议](#排错建议)

---

## 概述

Suba 的核心能力：

- 输入媒体文件，输出源语言字幕与目标语言字幕；
- 转录与翻译支持分阶段并发和设备策略；
- 支持 `Ollama` 与 `OpenAI` 两种翻译提供方；
- 通过 `TOML` 直接承载多行提示词，避免 JSON 转义问题；
- 发布时提供跨平台 `CPU` 包，及 `Windows/Linux` 的 `GPU` 包。

职责边界：

- **Suba（本仓库）**：音频准备、VAD、重叠检测、分离、转录、翻译、字幕写出；
- **外部依赖**：FFmpeg（音频提取）、ONNX Runtime（模型推理）、翻译服务（Ollama/OpenAI）。

---

## 处理流程

默认执行阶段如下：

1. `AudioPrepare`：必要时提取音频并统一到目标格式；
2. `VAD`：语音活动检测，切分语音段；
3. `OverlapResolve`：重叠检测与可选人声分离；
4. `Transcribe`：将语音段转录为源语言字幕；
5. `Translate`：将源语言字幕翻译为目标语言；
6. `SubtitleWrite`：写出源字幕与译文字幕。

运行时支持两种翻译调度模式：

- `PerTask`：单任务完成转录后立即翻译；
- `BatchAfterTranscription`：所有任务先完成转录，再统一翻译。

---

## 输入文件（TOML）

运行入口参数为一个 TOML 文件路径（`arguments-toml-path`）。  
TOML 文件仅包含三个字段：`inputs`、`prompt`、`fix_prompt`。

模板示例：

```toml
# 媒体输入文件列表（支持多个）
inputs = [
  "D:/Media/movie-001.mp4",
  "D:/Media/movie-002.mkv"
]

# 主翻译提示词（支持多行）
prompt = """
你是一个专业的字幕翻译助手。
只输出翻译后的文本，不要添加解释、标签或额外格式。
"""

# 翻译修复提示词（支持多行）
fix_prompt = """
你是翻译修复助手。
请将给定文本修正为一句最自然、最简洁的目标语言字幕，仅输出结果。
"""
```

初始化模板：

```bash
suba init
```

默认生成：`arguments.template.toml`

---

## 命令行用法

基础运行：

```bash
suba ./arguments.template.toml --ffmpeg-path "/usr/bin/ffmpeg" --models-root "./models"
```

Windows 示例：

```powershell
suba.exe .\arguments.template.toml --ffmpeg-path "C:\Program Files\ffmpeg\bin\ffmpeg.exe" --models-root ".\models"
```

常用组合：

```bash
# 使用 OpenAI 翻译
suba ./arguments.template.toml \
  --translation-provider OpenAi \
  --openai-api-key "YOUR_API_KEY" \
  --openai-model "gpt-4o-mini"

# 使用 Ollama 翻译
suba ./arguments.template.toml \
  --translation-provider Ollama \
  --ollama-base-url "http://localhost:11434" \
  --ollama-model "qwen3:14b"
```

---

## 参数说明（按阶段）

以下只列出高频和关键参数，完整参数请以 `suba --help` 为准。

### 1）全局与路径

- `arguments-toml-path`：任务输入 TOML 文件路径（必填）
- `--ffmpeg-path`：FFmpeg 可执行文件路径
- `--models-root`：模型根目录（默认 `./models`）
- `--cache-directory`：缓存目录
- `--log-directory`：日志目录
- `--console-log-level`：窗口日志等级
- `--file-log-level`：文件日志等级

### 2）运行时编排策略

- `--max-degree-of-parallelism`：全局最大并发度
- `--command-timeout-seconds`：命令超时秒数
- `--translation-mode`：翻译执行模式（`PerTask` / `BatchAfterTranscription`）
- `--gpu-conflict-policy`：GPU 冲突策略（按位组合）
- `--artifact-overwrite-policy`：字幕覆盖策略（默认不覆盖已有产物）

### 3）阶段设备与并发

- `--preprocess-device` / `--preprocess-parallelism`
- `--transcribe-device` / `--transcribe-parallelism`
- `--translate-device` / `--translate-parallelism`

说明：

- 若翻译提供方是 `OpenAi`（网络 API），翻译阶段设备配置不会触发本地 GPU 推理。

### 4）VAD 与重叠处理

- `--vad-threshold`
- `--vad-min-silence-ms`
- `--vad-min-speech-ms`
- `--vad-max-speech-seconds`
- `--vad-speech-pad-ms`
- `--overlap-detection-policy`
- `--overlap-onset` / `--overlap-offset`
- `--overlap-min-duration-on-seconds`
- `--overlap-min-duration-off-seconds`
- `--separated-vad-*`：分离后二次 VAD 参数
- `--sepformer-normalize-output`

### 5）转录参数

- `--transcribe-language-policy`（推荐 `Fixed`）
- `--transcribe-language`（默认 `ja`）
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

### 6）翻译参数

- `--translation-provider`：`Ollama` / `OpenAi`
- `--translate-language`：目标语言（BCP 47）
- `--translate-response-mode`：`NonStreaming` / `Streaming`
- `--translate-context-queue-size`
- `--translate-context-gap-ms`
- `--translate-partial-write-interval`：中间字幕落盘间隔，`0` 表示仅最终写出

Ollama 专属：

- `--ollama-base-url`
- `--ollama-model`

OpenAI 专属：

- `--openai-base-url`
- `--openai-api-key`（必填）
- `--openai-model`

### 7）字幕输出参数

- `--subtitle-format-policy`：字幕格式策略（默认 `Vtt`）

---

## 输出产物与命名规则

字幕命名规则：

```text
{源文件名（不含后缀）}.{语言标签}.{字幕后缀}
```

示例：

- `movie.ja.vtt`
- `movie.zh-CN.vtt`

翻译阶段中间产物使用 `.partial` 临时后缀，最终完成后会写出正式文件并清理临时文件。

---

## 发布包与平台矩阵

发布 workflow：`.github/workflows/publish-suba.yml`

CPU 包：

- `win-x64`
- `linux-x64`
- `osx-x64`

GPU 包：

- `win-x64`（包含最小 CUDA/cuDNN 依赖）
- `linux-x64`（包含最小 CUDA/cuDNN 依赖）

说明：

- `osx-x64` 不提供 CUDA/cuDNN GPU 包（macOS 不支持现代 CUDA 运行时）。

---

## 模型目录结构与下载坐标

`--models-root` 目录下必须满足以下结构（与代码校验逻辑一致）：

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

下载建议：

- `pyannote-segmentation-3.0`（ONNX）：  
  https://huggingface.co/onnx-community/pyannote-segmentation-3.0
- `kotoba-whisper-v2.2-ONNX`：  
  https://huggingface.co/onnx-community/kotoba-whisper-v2.2-ONNX
- `sepformer-wsj02mix`：本项目使用自导出的 ONNX 版本（`speechbrain/sepformer-wsj02mix/onnx/model.onnx`）。  
  该文件不直接由官方仓库以同路径发布，请使用项目发布包中的模型文件，或按项目导出流程自行导出后放置到上述目录。

说明：

- 目录层级与文件名需严格一致，否则启动时会触发模型校验失败；
- 仅替换同名文件即可升级模型，不需要修改命令行参数。

---

## GPU 依赖最小清单与目录结构

如果你所在网络下载 GPU 包较慢，可以先下载 `CPU` 包，再手动补齐 CUDA/cuDNN 依赖文件。

建议目录结构（zip 解压后）：

```text
suba/
  suba(.exe)
  arguments.template.toml
  README.md
  README.en.md
  onnxruntime* / libonnxruntime*
  <CUDA/cuDNN 依赖文件>
```

Windows（最小运行时文件）：

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

Linux（最小运行时文件）：

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

注意事项：

- 以上文件需与 `suba` 可执行文件放在同一目录；
- 版本口径为 CUDA 12 + cuDNN 9；
- 若只使用 CPU 推理，可直接使用 `CPU` 包，不需要以上依赖。

下载入口与包定位（手动补齐依赖时）：

- CUDA Redist 根目录：  
  https://developer.download.nvidia.com/compute/cuda/redist/
- cuDNN Redist 根目录：  
  https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/

Windows：

- `cudart64_12.dll`：`cuda_cudart/windows-x86_64/` 下 `cuda_cudart-windows-x86_64-*.zip`
- `cublas64_12.dll`、`cublasLt64_12.dll`：`libcublas/windows-x86_64/` 下 `libcublas-windows-x86_64-*.zip`
- `cufft64_11.dll`：`libcufft/windows-x86_64/` 下 `libcufft-windows-x86_64-*.zip`
- `cudnn*.dll`：`https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/windows-x86_64/` 下 `cudnn-windows-x86_64-*.zip`

Linux：

- `libcudart.so.12`：`cuda_cudart/linux-x86_64/` 下 `cuda_cudart-linux-x86_64-*.tar.xz`
- `libcublas.so.12`、`libcublasLt.so.12`：`libcublas/linux-x86_64/` 下 `libcublas-linux-x86_64-*.tar.xz`
- `libcufft.so.11`：`libcufft/linux-x86_64/` 下 `libcufft-linux-x86_64-*.tar.xz`
- `libcudnn*.so.9`：`https://developer.download.nvidia.com/compute/cudnn/redist/cudnn/linux-x86_64/` 下 `cudnn-linux-x86_64-*.tar.xz`

---

## 构建与测试

```bash
dotnet build Suba.sln -v minimal
dotnet test Suba.sln -v minimal
```

---

## 排错建议

1. ONNX 打印 `Some nodes were not assigned to the preferred execution providers`  
这是常见提示，通常不影响功能；项目已将 ONNX 日志级别抑制到 Error。

2. GPU 模型加载失败（缺少 CUDA/cuDNN 动态库）  
请使用对应平台的 `gpu` 发布包，或确保依赖库与可执行文件位于同目录。

3. 翻译耗时异常长  
优先检查 `prompt` 与 `fix_prompt` 是否过长、是否包含无关输出约束。
