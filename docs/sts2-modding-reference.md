# StS2 / BaseLib API reference (verified against decompiled source)

Paths are relative to `.decompiled/sts2/` or `.decompiled/BaseLib/` (gitignored; see AGENTS.md to regenerate).
Game version at time of writing: min_game_version 0.107.0, BaseLib 3.4.7. Re-verify after game updates.

## Character minimums (what makes a valid, non-crashing character)
| Item | Hard minimum | Recommended | Evidence |
|---|---|---|---|
| Card pool | ≥3 distinct non-Basic cards reachable per reward roll; rarity roll throws if pool has no valid rarity | ≥5 each Common/Uncommon/Rare to start; base chars have ~75–90 | `MegaCrit.Sts2.Core.Factories/CardFactory.cs` `CreateForReward` throws `InvalidOperationException` |
| Card types | ≥1 Attack, ≥1 Skill, ≥1 Power in pool (merchant rolls by type) | several of each | `CardFactory.cs` merchant generation |
| Starting deck | any | 10 cards: Strikes/Defends + 2 unique | `CharacterModel.StartingDeck` |
| Starting relic | any (template uses Ironclad's) | 1 unique | `CharacterModel.StartingRelics` |
| Relic pool | 0 OK (skipped) | 5–8 | |
| Potion pool | 0 OK | 2–3 | BaseLib default empty |
| Localization | all STS001 keys in `characters.json` + `ancients.json` | | analyzer |

**Current state:** 57 cards (3 Basic, 17 Common, 27 Uncommon, 10 Rare) + 4 special Token cards (`TokenCardPool`), 7 relics (1 starter), 3 potions. No status/curse cards at the moment.
- Status/curse cards: subclass `RunePriestCard` and override the pool with `[Pool(typeof(StatusCardPool))]` / `[Pool(typeof(CurseCardPool))]`; set `CanBeGeneratedInCombat => false`, `MaxUpgradeLevel => 0`, cost -1, keyword Unplayable. "When drawn" = `AfterCardDrawn(ctx, card, fromHandDraw)` with `card == this`.
- Generated cards: `CombatState.CreateCard<T>(Owner)` → `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Random)`; curse to deck: `CardPileCmd.AddCurseToDeck<T>(player)` in a relic's `AfterObtained` (+ `HasUponPickupEffect => true`).
- Max energy relic: override `ModifyMaxEnergy(Player, decimal)`. Potions: `PotionRarity`, `PotionUsage.CombatOnly`, `TargetType.AnyPlayer`, `OnUse(ctx, target)`.

## Models & registration (BaseLib)
- `BaseLib.Abstracts.CustomCardModel(int cost, CardType, CardRarity, TargetType, bool showInCardLibrary = true, bool autoAdd = true)`; `[Pool(typeof(RunePriestCardPool))]` on the base class registers subclasses.
- `BaseLib.Abstracts.ConstructedCardModel` — helpers `Damage(base, upgrade)`, `Block(base, upgrade)` for `CanonicalVars`.
- `BaseLib.Abstracts.CustomPowerModel` / `CustomTemporaryPowerModel`, `CustomRelicModel`, `CustomPotionModel`.
- `BaseLib.Abstracts.CustomSingletonModel(HookType.Combat)` — a model that receives combat hooks globally via `ModHelper.SubscribeForCombatStateHooks` (no owner creature).
- `BaseLib.Utils.SpireField<TKey,TVal>` — attach data to any object (ConditionalWeakTable); `.CopyOnClone()` to survive model cloning.
- Custom keyword: static field in any class
  ```csharp
  [CustomEnum("INSCRIBE")] [KeywordProperties(AutoKeywordPosition.After)]
  public static CardKeyword Inscribe;
  ```
  (see `BaseLib.Cards/BaseLibKeywords.cs`). Loc in `card_keywords.json` as `<ID>.title` / `<ID>.description`.
- Custom target types: `BaseLib.Patches.Features/CustomTargetType.cs`.

## Cards (`MegaCrit.Sts2.Core.Models.CardModel`)
- `protected virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)` — `cardPlay.Target` is the chosen `Creature?`.
- `protected virtual IEnumerable<DynamicVar> CanonicalVars` — `DamageVar(6m, ValueProp.Move)`, `BlockVar`, `PowerVar<T>(n)`, custom `DynamicVar(name, value)`. Access `DynamicVars.Damage`, `DynamicVars["Name"]`.
- `protected virtual void OnUpgrade()` → `DynamicVars.Damage.UpgradeValueBy(3m)`.
- `CanonicalTags` (`CardTag.Strike`, `CardTag.Defend`), keywords `CardKeyword.Exhaust/Ethereal/Retain/Innate/Unplayable`.
- Reference examples: `Models.Cards/StrikeIronclad.cs`, `DefendIronclad.cs`, `Inflame.cs`, `Whirlwind.cs` (all enemies), `BeatDown.cs` / `BouncingFlask.cs` (random enemy).
- Description text: SmartFormat, `"Deal {Damage:diff()} damage."`.

## Commands (`MegaCrit.Sts2.Core.Commands`)
- Attack: `DamageCmd.Attack(decimal).FromCard(card, play).Targeting(creature) | .TargetingAllOpponents(combatState)` `.WithHitCount(n).WithHitFx("vfx/vfx_attack_slash").Execute(ctx)`
- Non-attack damage: `CreatureCmd.Damage(ctx, target(s), decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)` (`CreatureCmd.cs:221/258`)
- Block: `CreatureCmd.GainBlock(Creature, decimal amount, ValueProp props, CardPlay?, bool fast=false)`
- Heal: `CreatureCmd.Heal(Creature, decimal amount, bool playAnim = true)`
- Energy: `PlayerCmd.GainEnergy(decimal, Player)`, `PlayerCmd.LoseEnergy`
- Powers: `PowerCmd.Apply<T>(ctx, Creature target, decimal amount, Creature? applier, CardModel? source, bool silent=false)`
- Next-turn powers exist: `EnergyNextTurnPower`, `DrawCardsNextTurnPower`, `BlockNextTurnPower`. Debuffs: `WeakPower`, `VulnerablePower`, `PoisonPower`, `DoomPower`. Stats: `StrengthPower`, `DexterityPower`, `FocusPower`.
- `ValueProp` flags (`MegaCrit.Sts2.Core.ValueProps`): `Unblockable=2, Unpowered=4, Move=8, SkipHurtAnim=0x10`. Presets in `DamageProps` / `BlockProps`: `card`, `cardUnpowered`, `cardHpLoss`, `nonCardUnpowered`, `nonCardHpLoss` (self HP loss = `nonCardHpLoss`).
- `VulnerablePower.ModifyDamageMultiplicative` only applies when `props.IsPoweredAttack()` — Unpowered damage ignores Vulnerable (and likely Weak/Strength).

## Combat state
- `card.CombatState` / `creature.CombatState` → `ICombatState`: `Enemies`, `HittableEnemies`, `PlayerCreatures`, `Players`, `RoundNumber`, `CurrentSide`.
- `player.PlayerCombatState` — piles, energy, `OrbQueue`.
- RNG: `Owner.RunState.Rng.CombatTargets.NextItem(list)` (`Runs/RunRngSet.cs`).

## Hooks (virtuals on `AbstractModel`, dispatched by `Hooks/Hook.cs`)
- Listeners: `CombatState.IterateHookListeners()` = all creature powers, player relics, potions, orbs, cards in piles, modifiers, + `ModHelper` subscribers.
- End of turn order: `BeforeSideTurnEndVeryEarly` → `BeforeSideTurnEndEarly` → `BeforeSideTurnEnd` → (orb `BeforeTurnEndOrbTrigger`, hand discard) → `AfterSideTurnEnd` → `AfterSideTurnEndLate`. Signature: `Task X(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)`.
- Each listener gets a `HookPlayerChoiceContext` bound to the model's owner — prefer player-owned models (powers/relics) for per-player logic in co-op.
- Hooks stop iterating once combat is ending.

## Orbs & overhead UI (pattern for the rune buffer UI)
- Model: `Models/OrbModel.cs` (`BeforeTurnEndOrbTrigger`, `Passive`, `Evoke`, `HoverTips`, `Icon`, `CreateSprite()`); storage `Entities.Orbs/OrbQueue.cs` on `PlayerCombatState.OrbQueue`.
- UI: `Nodes.Orbs/NOrbManager.cs` created in `Nodes.Combat/NCreature.cs` `_Ready()` for players (`NOrbManager.Create(this, LocalContext.IsMe(Entity))`), arc layout (`_minRadius 225`, `_maxRadius 300`), listens to `CombatManager.Instance.StateTracker.CombatStateChanged`. Individual `Nodes.Orbs/NOrb.cs` extends `NClickableControl`.
- Anchor points: `NCreatureVisuals.OrbPosition`, `IntentPosition`, `TalkPosition`, `VfxSpawnPosition`.
- Hover tips: `HoverTips/HoverTip.cs` (`HoverTip(LocString title, LocString description, Texture2D? icon)` variants), rendered by `Nodes.HoverTips/NHoverTipSet`.
- The Regent's Sovereign Blade is a Spine animation state (`Models.Characters/Regent.cs`), not a reusable floating node.
- Mod attachment approach: Harmony postfix on `NCreature._Ready` → `AddChild(NRuneBuffer)` when the creature is a player.

## Localization
- `new LocString(table, key)`, `.Add(name, value)`, `.GetFormattedText()`; `LocString.Exists(table, key)`.
- Tables = file names in `RunePriest/localization/eng/`: `cards`, `powers`, `relics`, `potions`, `characters`, `ancients`, `card_keywords`, `static_hover_tips`.

## RunePriest–Specific Card Types

**Rune Priest introduces new card categories** that use existing `CardType` and `CardRarity` enums:

| Type | Rarity | Folder | Base Class | Example | Notes |
|------|--------|--------|-----------|---------|-------|
| **Standard** | Basic/Common/Uncommon/Rare | `Basic/Common/Uncommon/Rare/` | `RuneCard` or `RunePriestCard` | Strike Rune | Regular rune-casting cards |
| **Token** | Token | `Token/` | `RunePriestCard` | Black Mark | Generated only (Curse-like), never in rewards |
| **Ancient** | Ancient | `Ancient/` | `RuneCard` or `RunePriestCard` | Annihilation | Ancient boon cards (full-art frame, never in rewards); Ascended cards are Ancient with `MaxUpgradeLevel => 0` |
| **Quest** | Rare | `Quest/` | `RunePriestCard` | TBD | Multi-stage cards or milestone mechanics; planned for future |
| **Event** | — | `Event/` | Event model (TBD) | TBD | Map event reward cards; planned for future |

**Card pool organization:**
- `[Pool(typeof(RunePriestCardPool))]` — standard cards (only rarity-gated rewards)
- `[Pool(typeof(TokenCardPool))]` — generated cards (invisible to rewards, shows up via card-creating effects)

**Ascended cards**: `CardRarity.Ancient` + `MaxUpgradeLevel => 0`; reached through Orobas' Archaic Tooth (see
[rune-system-design.md §15](rune-system-design.md#15-ancients-and-ascended-cards)).

## Ancient boons (RunePriest-specific)

Ancient options are **relics**. See [docs/custom-ancients.md](custom-ancients.md).

- Orobas Touch of Orobas: starter relic's `CustomRelicModel.GetUpgradeReplacement()` (BaseLib).
- Orobas Archaic Tooth: BaseLib `ITranscendenceCard` on the starting card(s).
- Darv Dusty Tome: BaseLib `ITomeCard` on the character's tome card.
- Any other vanilla ancient: Harmony postfix on its `GenerateInitialOptions` (and `AllPossibleOptions` getter, for the
  relic collection / dev console) — see `Patches/AncientOptionPatches.cs`. Option text falls back to the relic's
  title and `description`/`eventDescription`, so no `ancients.json` keys are needed.

## Custom Events (RunePriest-specific)

Events are non-combat map encounters offering rewards or challenges. Custom events can be character-locked:

- **Base pattern:** Extend event base and use `RunePriestEventMixin` (see [docs/custom-events.md](custom-events.md))
- **Filtering:** Use `IsRunePriestOnly()` to hide generic events from RunePriest or lock RunePriest events to RunePriest
- **Results:** Event options can grant cards, relics, runes, or powers
- **Localization:** Keys in `events.json` as `RUNEPRIEST_<KEY>.*`

**To investigate:** Does BaseLib provide `CustomEventModel`? If not, pattern matches event-generation Harmony patch (similar to ancient integration).

---

## Investigation Checklist for Future Versions

- [x] BaseLib 3.4.7 has `CustomAncientModel` (whole new ancients) but no hook for adding options to vanilla ancients
- [ ] BaseLib: check for `CustomEventModel` support
- [ ] Game version 0.112+: Verify ancient/event APIs haven't changed
- [ ] CI build: Ensure `UseSts2RefAssemblies=true` compiles without decompiled dependencies

