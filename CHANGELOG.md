# Changelog

All notable changes to this project *from CementGB v4.5.0 onwards* will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project attempts to adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.6.1]

### Added

- All you early map-makers to the Credits menu. (Thank you @minimack_studios, @konradkg25, @rafa120hojaz, @lozawasntaken, @reversedhuman)

### Changed

- Add extra null checks to SceneInfo retrievals to protect against rare circumstances where SceneInfo might be null

### Fixed

- Random selections sometimes picked the Modded selection as an individual map, causing softlocks. This is now fixed. ([#49](https://github.com/CementGB-4-0/CementSource/issues/49))
- Embedded path to CreditsText.txt was incorrect in the DisplayCredits patch, making the Credits menu completely blank. The path is now fixed.

## [4.6.0]

### Added

- Waves support.

### Changed

- Bumped recommended MelonLoader version to v0.7.3
- Bumped recommended Il2CppInterop version to v1.5.1
- Bumped recommended Tomlet version to v6.1.0

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

[4.6.1]: https://github.com/CementGB-4-0/CementSource/compare/v4.5.0...v4.6.1
[4.6.0]: https://github.com/CementGB-4-0/CementSource/compare/v4.5.0...v4.6.0
[4.5.0]: https://github.com/CementGB-4-0/CementSource/compare/v4.4.0...v4.5.0
