# Changelog

All notable changes to this project *from CementGB v4.5.0 onwards* will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project attempts to adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.5.0]

### Added

- Reimplementation of Boneloaf and Rocket Science splash screen skipping. Will automatically skip if `-map` or new `-ss`
  launch options are provided. *Later, this will also work automatically for NetBeard servers.*
- "Random" map selection in `-map` launch option.
- "Modded" map selection in lobby menu and `-map` launch option. Plays all found modded maps in random succession.

### Changed

- Recommended MelonLoader version is now v0.7.2
- Custom `SceneAudioConfig` fallbacks and fixes are now handled in a patch to `SceneLoader::OnSceneLoaded`

### Fixed

- Update README to include corrected CementGB.github.io link
- Unload any additional builtin shader bundles and retry scene load when custom maps fail. This should solve most issues
  related to infinite loading.
- Add newly created Cement modules to ModuleHolder earlier to prevent early logging issues

[4.5.0]: https://github.com/CementGB-4-0/CementSource/compare/v4.4.0...v4.5.0