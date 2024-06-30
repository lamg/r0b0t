# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.7] - 2024-06-30

Added:

- Images saved to the directory where `r0b0t` is running now contain the following metadata:
  - `prompt`: the prompt used to generate the image
  - `revised_prompt`: the revised prompt returned by `dall-e-3` after generating the image

## [0.0.6] - 2024-06-27

Fixed:

- HuggingFace module setting is no longer ignored.

## [0.0.5] - 2024-06-27

Added:

- Support for [server-sent events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events)

Fixed:

- Streaming text from Anthropic.
- Streaming text from HuggingFace

## [0.0.4] - 2024-06-25

Fixed:

- Configuration serialization.

## [0.0.3] - 2024-06-22

Added:

- Basic support for HuggingFace Inference API.

## [0.0.2] - 2024-06-22

Changed:

- Fixed README.md file to improve presentation in Nuget.org

## [0.0.1] - 2024-06-22

- Initial Nuget.org release
- OpenAI GPT4o, Dalle 3, GPT 3.5 support
- Github Copilot support
- Anthropic Haiku, Sonnet 3, Opus 3 support.
