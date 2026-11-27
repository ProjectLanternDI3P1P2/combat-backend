# Changelog

## [3.0.0](https://github.com/ProjectLanternDI3P1P2/combat-backend/compare/contracts-v2.0.0...contracts-v3.0.0) (2026-11-27)


### Added

* **combat:** open a combat over the internal gRPC contract ([faa8dda](https://github.com/ProjectLanternDI3P1P2/combat-backend/commit/faa8dda9ff63bffbcbc180afe2a22fdf346bb140))
* **contracts:** carry the run, the party and the monsters in CreateCombat ([314efba](https://github.com/ProjectLanternDI3P1P2/combat-backend/commit/314efbac7d6c424423e096e94df7beafe1d6d215))

## [2.0.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/contracts-v1.0.0...contracts-v2.0.0) (2026-09-23)


### Added

* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))

## 1.0.0 (2026-09-22)


### ⚠ BREAKING CHANGES

* **contracts:** release protobuf contracts independently

### Added

* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))

## Changelog

All notable changes to the Protocol Buffer contracts are documented in this file.

Contract releases use the form `Vn` (`n.0.0` in the NuGet package and the
`contracts-vn.0.0` Git tag). A release is created only when files under
`Combat.Contracts/` change.
