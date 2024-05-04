module SelectProviderModel

open Controls

open Types

let getActiveAnswerer (c: Config) =
  c.providers[c.active.provider].modelAnswerer c.active.model

let getActiveModels (c: Config) = c.providers[c.active.provider].models

let setProviderModel (c: Config) (spm: SetProviderModel) (pm: ProviderModel) =
  spm pm.provider pm.model
  { c with active = pm }
