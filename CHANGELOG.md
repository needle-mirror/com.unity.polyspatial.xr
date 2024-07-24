---
uid: psxr-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0-pre.9] - 2024-07-24

### Added
- Added support for tracked images over Play To Device.
- Added support for XR Meshes over Play To Device.
- Added Project Validation checks to enable the PolySpatial XR Plug-in Provider when Play To Device is enabled.
- Added Project Validation checks to disable the XR Simulation Plug-in Provider when Play To Device is enabled.

### Fixed
- Fixed PolySpatial XR in Play To Device on TestFlight builds.  The PolySpatial XR name space was getting stripped out.
- Fixed hands over P2D using PolySpatial XR.  Added INCLUDE_UNITY_XR_HANDS define to Unity.PolySpatial.XR.asmdef.

## [2.0.0-pre.3] - 2024-04-22

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.1.4] - 2024-02-26

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.1.3] - 2024-02-22

### Added

### Changed
- Update min Unity version to 2022.3.19f1

### Deprecated

### Removed

### Fixed

### Security

## [1.1.2] - 2024-02-21

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.1.1] - 2024-02-15

### Added
- Added object rotation for XR Touch Space Interactor based on the Spatial Pointer State input device rotation.
### Changed
- Renamed the Capability Profiles to match the App Mode options in the Apple VisionOS plug-in settings.

### Deprecated

### Removed

### Fixed

### Security

## [1.0.3] - 2024-01-20

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.7.1] - 2023-12-13

### Added

### Changed
- Require XR interaction toolkit 2.5.2.
- All packages now require 2022.3.15f1 and later (rather than 2022.3.11f1 and later) to pick up fixes for various memory leaks made in 15f1.

### Deprecated

### Removed
- Removed statistics window from the menu.
- Support for Unity versions earlier than 2022.3.11f1.

### Fixed

### Security

## [0.6.3] - 2023-11-28

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.2] - 2023-11-13

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.1] - 2023-11-09

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.0] - 2023-11-08

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.5.0] - 2023-10-26

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.4.3] - 2023-10-13

## [0.4.2] - 2023-10-12

## [0.4.1] - 2023-10-06

## [0.4.0] - 2023-10-04

### Added
- PolySpatial now supports Xcode 15.1 beta 1 and visionOS 1.0 beta 4

## [0.3.2] - 2023-09-18

## [0.3.1] - 2023-09-15

## [0.3.0] - 2023-09-13

### Added
- Bump minimum version for core-utils to 2.4.0-exp.3

## [0.2.2] - 2023-08-28

## [0.2.1] - 2023-08-25

## [0.2.0] - 2023-08-21

## [0.1.2] - 2023-08-16

## [0.1.0] - 2023-07-19

## [0.0.4] - 2023-07-18

## [0.0.3] - 2023-07-18

## [0.0.2] - 2023-07-17

## [0.0.1] - 2023-07-14

### Added
- Initial PolySpatial XR Extensions package.

