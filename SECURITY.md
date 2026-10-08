# Security Policy

LegionChromaFlow talks to your keyboard through the Windows HID API and reads
the pixels of the foreground window. By default it never opens network
connections and never sends any data anywhere.

The only exception is the optional **AI scenes** feature (off by default, needs
your own Anthropic API key): when you enable it, a small JPEG screenshot of the
active window is sent to `api.anthropic.com` when you switch window, at most once
per the configured minimum interval, and windows whose title matches
`AiSkipTitles` are never sent. The API key is stored encrypted with Windows
DPAPI (`config/ai.key`, git-ignored) or read from `ANTHROPIC_API_KEY`.

## Verifying what you run

* The source in this repository is the single source of truth. Official release
  binaries are built by [GitHub Actions](.github/workflows/release.yml) from a
  tagged commit, and each release ships a `SHA256SUMS.txt`.
* Only builds published on the **Releases** page of this repository are
  official. Binaries from forks, mirrors, or re-uploads are **not** — build from
  source (`build.bat`) if in doubt.
* Only the repository owner can merge into `main`. Pull requests are reviewed
  line by line before merging.

## Reporting a vulnerability

Please use **GitHub → Security → Report a vulnerability** (private advisory)
instead of opening a public issue. You will get a reply as soon as possible.
