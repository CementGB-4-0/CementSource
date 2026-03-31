# Changelog

All notable changes to this project *from CementGB v4.5.0 onwards* will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project attempts to adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.5.0]

### Added

- "Modded" map selection in lobby menu and launch args. Plays all found modded maps in random succession.

### Changed

- Update README to include corrected CementGB.github.io link
- Recommended MelonLoader version is now v0.7.2

### Fixed

- Unload builtin shaders and retry when custom maps fail to load. This should solve most issues related to infinite loading.
- Add newly created Cement modules to ModuleHolder earlier to prevent early logging issues

[4.5.0]: https://github.com/CementGB-4-0/CementSource/compare/v4.4.0...v4.5.0