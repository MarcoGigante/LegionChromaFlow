# Security Policy

LegionChromaFlow talks to your keyboard through the Windows HID API and reads
the pixels of the foreground window. It never opens network connections, and it
never sends any data anywhere.

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
