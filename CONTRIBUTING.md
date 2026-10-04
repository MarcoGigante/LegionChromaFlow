# Contributing

Thanks for your interest! Bug reports, hardware reports and pull requests are welcome.

* **Hardware reports are very valuable.** If you run it on a Legion model other
  than the 7 16IRX9, open an issue with the output of `probe.bat`
  (`dotnet bin\LegionChromaFlow.dll probe`) and say whether `test.bat` worked.
* Keep pull requests small and focused. Explain *why* in the description.
* Run `dotnet bin\LegionChromaFlow.dll selftest` before submitting; it must pass.
* No new dependencies. The project intentionally has none.
* All pull requests are reviewed by the maintainer before merging. Nothing is
  merged without review, and release binaries are only built by the maintainer's
  tagged workflow.

By contributing you agree that your work is licensed under GPL-3.0-or-later.
