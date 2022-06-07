# [1.5.0](https://github.com/SolidAlloy/ExtEvents/compare/1.4.0...1.5.0) (2022-06-07)


### Features

* Added ability to subscribe with methods that require implicit conversion of argument types ([ade6565](https://github.com/SolidAlloy/ExtEvents/commit/ade6565d0a7bdfc86e5a702e8e908644cc19425b))

# [1.4.0](https://github.com/SolidAlloy/ExtEvents/compare/1.3.1...1.4.0) (2022-05-19)


### Bug Fixes

* Fixed "+" icons randomly disappearing from inspector ([57aa96f](https://github.com/SolidAlloy/ExtEvents/commit/57aa96ff73095203f36fe414fac4ca30f1b85fed))
* Fixed Unity.Object references lost in serialized arguments ([98e23d1](https://github.com/SolidAlloy/ExtEvents/commit/98e23d1d3a18b21e290fab557b715585d75acfb4))


### Features

* Switched from JsonUtility to OdinSerializer for serialized persistent arguments ([5761652](https://github.com/SolidAlloy/ExtEvents/commit/57616529be8bc86a874732732769a01c1877c5c0))

## [1.3.1](https://github.com/SolidAlloy/ExtEvents/compare/1.3.0...1.3.1) (2022-05-13)


### Bug Fixes

* Fixed arguments of abstract types not being considered serializable ([633c655](https://github.com/SolidAlloy/ExtEvents/commit/633c655905be9cdd0831fc99c0dc3aa0ef5492ad))

# [1.3.0](https://github.com/SolidAlloy/ExtEvents/compare/1.2.2...1.3.0) (2022-05-13)


### Bug Fixes

* Applied the correct method of getting IL2CPP code generation in Unity 2022 ([d87beed](https://github.com/SolidAlloy/ExtEvents/commit/d87beed6e029332ae0913a1b76fff1ddf9a2bf74))
* Fixed "No Script Asset for ..." warning appearing when changing serialized argument value in Play Mode ([366e585](https://github.com/SolidAlloy/ExtEvents/commit/366e5851dd644938e7dbc2f9a7c9233e4178c301))
* Fixed add button icons missing after exiting play mode ([0ed9c9e](https://github.com/SolidAlloy/ExtEvents/commit/0ed9c9e1935f0940ca61d1f3562d120662ecbc8e))
* Fixed old serialized values passed to event listeners after changing in the inspector ([ed7aa78](https://github.com/SolidAlloy/ExtEvents/commit/ed7aa78510b7ebe321a5ec79cf74c3ed9ed664be))


### Features

* Added GameObject to the list of components that can be added as instance listeners ([4c02b52](https://github.com/SolidAlloy/ExtEvents/commit/4c02b52b94cdf0bf8e6bfe22e4d31659f5297bcd))

## [1.2.2](https://github.com/SolidAlloy/ExtEvents/compare/1.2.1...1.2.2) (2022-05-10)


### Bug Fixes

* Fixed overflow issue with nested serialized arguments ([31c8a16](https://github.com/SolidAlloy/ExtEvents/commit/31c8a16bcaeb1174e9611fe49efb77fcdb9a480f))

## [1.2.1](https://github.com/SolidAlloy/ExtEvents/compare/1.2.0...1.2.1) (2022-05-02)


### Bug Fixes

* Fixed incorrect handling of serialized value-typed values ([1001a06](https://github.com/SolidAlloy/ExtEvents/commit/1001a06f121d5bf52eaa623fa430145c6d740cc5))

# [1.2.0](https://github.com/SolidAlloy/ExtEvents/compare/1.1.0...1.2.0) (2022-05-02)


### Bug Fixes

* Fixed a ReorderableList exception in Unity 2021.2.15 ([b78fcdc](https://github.com/SolidAlloy/ExtEvents/commit/b78fcdc015bcfaa2b6e61a077336e8c654e105a5))
* Fixed missing property in Unity 2021.1 ([2944c58](https://github.com/SolidAlloy/ExtEvents/commit/2944c5804caed31701b4c52bba1161af089812fa))
* Fixed ReorderableList methods not being found through reflection ([e00881f](https://github.com/SolidAlloy/ExtEvents/commit/e00881f78abfb170d9e091c391d9201e7d31706d))
* Started suppressing the missing type warning when a method argument of such type no longer exists ([4c28909](https://github.com/SolidAlloy/ExtEvents/commit/4c2890926640551206a194a657dad3854d11aeb4))


### Features

* Replaced an enum button with a single-click button to switch between dynamic and serialized ([20701a4](https://github.com/SolidAlloy/ExtEvents/commit/20701a47dce745f5f38329623eabab900462664c))
* Replaced object with void* to avoid boxing of structs ([8c4ffa0](https://github.com/SolidAlloy/ExtEvents/commit/8c4ffa06217eb4657914270f5b292b5ef434906d))

# [1.1.0](https://github.com/SolidAlloy/ExtEvents/compare/1.0.3...1.1.0) (2022-02-13)


### Bug Fixes

* Fixed NullReferenceException in Unity 2020.2 and earlier ([b86b8b4](https://github.com/SolidAlloy/ExtEvents/commit/b86b8b4189e7d8501d608ce47cc83146ae7b7ad3))


### Features

* Made PackageSettings public ([acb4291](https://github.com/SolidAlloy/ExtEvents/commit/acb429188a74a83afc41bfd2a046c1845f322d82))

## [1.0.3](https://github.com/SolidAlloy/ExtEvents/compare/1.0.2...1.0.3) (2022-02-03)


### Bug Fixes

* Fixed the error in console regarding the immutable Changelog file ([c418383](https://github.com/SolidAlloy/ExtEvents/commit/c418383a30b5b0a82512a973f095401bb1d3874c))

## [1.0.2](https://github.com/SolidAlloy/ExtEvents/compare/1.0.1...1.0.2) (2022-02-03)


### Bug Fixes

* Fixed the package name in package.json ([5eca665](https://github.com/SolidAlloy/ExtEvents/commit/5eca665afc6bc1de4beb7916c81cf9ddc7bb2e73))

## [1.0.1](https://github.com/SolidAlloy/ExtEvents/compare/1.0.0...1.0.1) (2022-02-03)


### Bug Fixes

* Updated the dependencies versions ([bd9468f](https://github.com/SolidAlloy/ExtEvents/commit/bd9468f31e2bc4ee678ec136ed56dc18e100b7f8))

# 1.0.0 (2022-02-03)


### Features

* Added semantic versioning ([15b0b67](https://github.com/SolidAlloy/ExtEvents/commit/15b0b67353d1adf3643b57b9e617330dc0d59c5b))
* First plugin release ([0585318](https://github.com/SolidAlloy/ExtEvents/commit/058531809cd85fbd4987563e434f414f28d09e33))
* Released the plugin ([89efa78](https://github.com/SolidAlloy/ExtEvents/commit/89efa784230bce8cf9d915aaac20f97b700d7528))
* Released the plugin ([7e5d2c0](https://github.com/SolidAlloy/ExtEvents/commit/7e5d2c08614b074689aee5bc036d6cbeeb9f27ef))
