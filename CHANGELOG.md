# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

# [0.1.6] - 2024-08-31

Added:

- Support for Perplexity, using environment variable `perplexity_key` for the API key
- The supported models are `llama-3.1-sonar-small-128k-online`, `llama-3.1-sonar-large-128k-online` and `llama-3.1-sonar-huge-128k-online`.


# [0.1.5] - 2024-08-25

Fixed:

- Storing configuration when a setting changes (provider, model, API key)
- Avoiding null string when reading server sent events

# [0.1.4] - 2024-08-24

Changed:

- configuration moved from $HOME/.config/r0b0t.json to $HOME/.config/r0b0t/conf.json
- internal design based on events, moving event handlers to GtkGui module and simplifying @lanayx style dependency injection, eliminating it for the Core module. Navigation was simplified by relying on fixed configuration groups rather than a tree.

Fixed:

- Display of existing API keys in command palette

# [0.1.3] - 2024-08-09

Added:

- Progress display when waiting for images

# [0.1.2] - 2024-08-09

Added:

- Set API keys using GUI

# [0.1.1] - 2024-08-08

Fixed:

- refactoring requestProcessor and not asking for completion while busy

# [0.1] - 2024-08-08

Added:

- Full rewrite using dependency injection a la @lanayx
- GTK4 GUI (using gir.core)
- Midjourney support using https://imaginepro.ai API
- Command palette with simplified navigation
- Introduction message

## [0.0.8] - 2024-07-19

Added:

- Support for [GPT4o-mini](https://openai.com/index/gpt-4o-mini-advancing-cost-efficient-intelligence/)

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
- GitHub Copilot support
- Anthropic Haiku, Sonnet 3, Opus 3 support.
