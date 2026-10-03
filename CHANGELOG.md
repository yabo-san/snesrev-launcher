# Changelog

## [1.6.2](https://github.com/yabo-san/snesrev-launcher/compare/v1.6.1...v1.6.2) (2026-10-03)


### Bug Fixes

* **kit:** add ../assets to the embedded Python's ._pth; isolated mode dropped the script dir so restool's import util failed (v1.6.1 on a real PC) ([2c4ecce](https://github.com/yabo-san/snesrev-launcher/commit/2c4ecceb5b7d49264f6aade8cffe708fe6d8aff9))
* **kit:** embedded Python could not import the asset scripts ([3fc051e](https://github.com/yabo-san/snesrev-launcher/commit/3fc051eb6b3921da2f5a8c199668d3d1f664bb4c))

## [1.6.1](https://github.com/yabo-san/snesrev-launcher/compare/v1.6.0...v1.6.1) (2026-10-02)


### Bug Fixes

* **build:** drop the LibGit2Sharp reference the 1.6.0 release commit re-added ([140eb76](https://github.com/yabo-san/snesrev-launcher/commit/140eb76606580f116a54b95a933eda7c326a91a7))
* **build:** drop the LibGit2Sharp reference the 1.6.0 release commit re-added; restore --locked-mode failed and v1.6.0 shipped without the launcher exe ([b39d5a9](https://github.com/yabo-san/snesrev-launcher/commit/b39d5a91d329265c30570d8be8fb20cd1b7ee857))

## [1.6.0](https://github.com/yabo-san/snesrev-launcher/compare/v1.5.1...v1.6.0) (2026-10-02)


### Features

* CI-verified build kit per game; no git, no tool downloads on the user's PC ([5edb9eb](https://github.com/yabo-san/snesrev-launcher/commit/5edb9eb23badc614f5f5f13e2abd8664fb8d9997))

## [1.5.1](https://github.com/yabo-san/snesrev-launcher/compare/v1.5.0...v1.5.1) (2026-10-02)


### Bug Fixes

* ROM search off the UI thread (hitch after choosing the ROM folder) ([ca17151](https://github.com/yabo-san/snesrev-launcher/commit/ca1715122086127168998bd011ce3c6083e7681d))
* ROM search runs off the UI thread; the window stayed frozen after choosing the ROM folder ([e1fc186](https://github.com/yabo-san/snesrev-launcher/commit/e1fc1864dc7921d444029d8665da61ec8046e355))

## [1.5.0](https://github.com/yabo-san/snesrev-launcher/compare/v1.4.0...v1.5.0) (2026-10-02)


### Features

* Super Mario Bros. and The Lost Levels from the All-Stars ROM; Super Mario World's asset step ([237c0e1](https://github.com/yabo-san/snesrev-launcher/commit/237c0e1573fb0f92b71bfa1e02717561acd0d4e6))
* Super Mario World's asset step; All-Stars deliberately not set up ([69a4a61](https://github.com/yabo-san/snesrev-launcher/commit/69a4a6169fae79f028d9907a77e42d12fe16025b))
* Super Mario World's asset step; All-Stars deliberately not set up ([036b1ad](https://github.com/yabo-san/snesrev-launcher/commit/036b1ad1a62ef15895550171319cb63f395cbf8d))

## [1.4.0](https://github.com/yabo-san/snesrev-launcher/compare/v1.3.6...v1.4.0) (2026-10-02)


### Features

* one launcher for zelda3, Super Metroid and Super Mario World; ROM found by hash; no UI freezes ([c870b83](https://github.com/yabo-san/snesrev-launcher/commit/c870b83040f64d1a9278782878874fd51bcc9302))


### Bug Fixes

* settings form no longer pegs a core while waiting on a process ([5c77ebb](https://github.com/yabo-san/snesrev-launcher/commit/5c77ebb0e5bf465697bb40538e043dd4e561f8b4))
