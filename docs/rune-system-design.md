# Rune System Design — "The Incantation"

Status: **v0.5 — first designed card set** (rune core, 48 cards from the design CSVs, 6 relics, 3 potions, Imbue, overhead UI, capacity, forecast).
Untested in-game; all numbers are first-pass. Update this doc when decisions change.

## 1. Pitch
Noita lets you build wands from spell tiles that act as a block-coding language. The Rune Priest reframes that for
StS2: cards **Inscribe** glyphs into an ordered buffer (the **Incantation**) floating above the character. At end of
turn the Incantation is **Spoken** — evaluated left→right as a tiny program — then cleared. Cards are cheap building
blocks; the power comes from *ordering* them. Malformed programs never crash: bad pieces **Fizzle** with a puff of
chalk dust and the rest keeps going.

## 2. Terminology
| Term | Meaning | Code |
|---|---|---|
| **Rune** | Atomic instruction: `Strike 5`, `Loop 2`, `Scatter`. | `Rune` |
| **Glyph** | *Code-only term.* What one card inscribes into one slot. Usually 1 rune; **compound** glyphs bundle several payload runes (e.g. `Strike 14, Blood 3`). A card may inscribe several glyphs. **Player-facing text always says "rune"** (a compound glyph counts as one rune). | `Glyph` |
| **Incantation** | The ordered glyph buffer above the head. Unlimited by default; content can impose a **capacity** (§13). | `RuneBuffer` |
| **Inscribe** | Append glyph(s) to the Incantation. Shown as a static hover tip (`RuneTips.Inscribe`), not a `CardKeyword`. | `RuneCmd.Inscribe` |
| **Speak** | Evaluate the Incantation. Happens automatically at end of turn; the **Invoke** card speaks it mid-turn. | `RuneCmd.Speak` |
| **Fizzle** | A glyph that can't do anything dissolves harmlessly (logged as `[Rune] ... fizzles`). | `RuneInterpreter` |

## 3. Rune categories
Every glyph has exactly one **kind**, determined by its runes:

1. **Payload glyph** — one or more payload runes (effects). The only kind that does something directly.
2. **Modifier glyph** — alters the *next* glyph.
3. **Target glyph** — sets the targeting mode for all later payloads.
4. **Flow glyph** — control flow (loops, seal).

Compound glyphs may only combine *payload* runes (optionally with a baked-in anchor target). Modifier/Target/Flow runes
are always their own glyph (a card can inscribe several glyphs, e.g. `[Loop 2][Strike 4][End]`). This keeps the
grammar trivially parseable and the visuals readable.

**Merging:** when a cast's *first* glyph matches the glyph currently at the end of the Incantation (same kind, same
runes in the same order, same anchor), they merge and values add: `[Strike 6]` + Strike Rune → `[Strike 12]`,
`[Strike 14 + Blood 3]` × 2 → `[Strike 28 + Blood 6]`, `[Loop 2]` + `[Loop 2]` → `[Loop 4]`, Echo + Echo → Echo +2.
Mergeable runes implement `Rune.WithValue`; targets, End Loop, Seal and Sanctify never merge. A single card's own
glyph sequence (Double Stroke's two Strikes) stays separate, so multi-hit cards keep their per-hit behaviour.
Merging does not use a capacity slot.

## 4. Base rune set (v1)

### Payloads
| Rune | Effect | Scalable¹ | Notes |
|---|---|---|---|
| **Strike X** | Deal X damage to the current target(s). | ✔ | Powered attack (`ValueProp.Move`) so Vulnerable/Weak/Strength apply per hit, like multi-hit attacks (see §7). |
| **Defend X** | Gain X Block. | ✔ | Powered (`ValueProp.Move`) so Dexterity/Frail apply. Block at end of turn still protects during enemy turn. |
| **Mend X** | Heal X HP. | ✔ | Strong in StS — keep rare, low values, often Exhaust. |
| **Strength X** | Grant X Strength to the target ally (you by default). | ✔ | Ally-targeted so Nova shares it in co-op. |
| **Hex X** | Apply X Weak **and** X Vulnerable to target(s). | ✔ | Order matters: Hex before Strike amplifies later hits. |
| **Cleanse** | Remove all debuffs from the target ally. | ✘ | Valueless (internally 1); never merges. |
| **Kindle X** | Gain X Energy. | ✘ | End-of-turn → `EnergyNextTurnPower`. Invoked → immediate. |
| **Swift X** | Draw X cards. | ✘ | End-of-turn → `DrawCardsNextTurnPower`. Invoked → immediate. |
| **Blood X** | Lose X HP (unblockable, self). | ✔ | Engine-only for now (no card inscribes it); the drawback half of compound glyphs. |

¹ *Scalable* payloads are affected by Amplify/Twin/Imbue. Non-scalable ones still repeat in loops.

### Imbue (instant, not a rune)
**Imbue X** empowers the X most recently inscribed glyphs that *can* be imbued (payload glyphs with a scalable rune):
each scalable value gains `RuneCmd.ImbueBonus` (+2) and the glyph's `Imbued` counter goes up. Values are rewritten in
place (`Glyph.Imbue`, `buffer.Replace` → the overhead glyph pulses), so Imbue is "Loop-immune": it is not a modifier and
does not get multiplied by loops. Listeners get `AfterImbued` per glyph (Paladin Sigil). Cards: Blank Rune, Spellbook,
Transmute, Ancient Tablet (`ImbueAll`), Holy Smite, Alchemize+.

### Modifiers (apply to the **next glyph**)
| Rune | Effect |
|---|---|
| **Amplify +X** | +X to every scalable payload in the next glyph. |
| **Twin ×N** | Multiply every scalable payload in the next glyph by N (cards inscribe ×2; two Twins merge to ×4). |
| **Echo** | Execute the next glyph one extra time. |
| **Sanctify** | Blood runes in the next glyph (or whole loop body) resolve as 0. Engine-only. |

Rules:
- Pending modifiers **stack and apply in inscription order**: `Amplify 3, Twin ×2, Strike 5` → (5+3)×2 = 16; `Twin ×2, Amplify 3, Strike 5` → 5×2+3 = 13. Ordering *is* the skill expression.
- A modifier applies to **all** scalable payloads in the glyph — including `Blood`. Doubling `Strike 14 + Blood 3` doubles both.
- **Target glyphs are transparent**: modifiers pass through them to the following glyph.
- If the next glyph is a **Loop**, the modifier applies to **the entire loop body on every iteration**.
- A modifier with nothing valid after it (end of Incantation, `Seal`) **fizzles**.
- Loop boundaries are transparent: a modifier at the end of a loop body **rolls over** to the first glyph of the next iteration (`[Loop 3][Strike 6][Echo]` → iterations 2 and 3 strike twice), and after the last iteration to the glyph after the loop. Target modes already persist across iterations.

### Targets (persist until changed)
Each payload rune declares a `RuneTargeting`: **Enemy** (Strike, Hex), **Ally** (Defend, Mend, Strength, Cleanse), or **Self**
(Blood, Kindle, Swift — always the caster, ignores target mode). The mode picks from the relevant side:

| Rune | Enemy payloads | Ally payloads |
|---|---|---|
| *(default)* **Anchor** | The enemy targeted when the glyph's card was played; if dead/none → random enemy. | The ally targeted by the card; if none → the caster. |
| **Scatter** | Random enemy, re-rolled **per execution** (loops scatter). | Random ally. |
| **Nova** | All enemies. | All allies. |
| **Execution** | Lowest current HP enemy. | Lowest current HP ally (good with Mend). |
| **Mirror** *(curse rune)* | **You.** | A random enemy. |

Mirror is a mode like the others: it lasts until the next targeting rune, so any target-control card counters it.
`[Mirror][Strike][Block][Strike]` → both Strikes hit you and the Block goes to an enemy.

Mirror is only for curses/status/enemy effects — no player card inscribes it yet.

### Flow
| Rune | Effect |
|---|---|
| **Loop N** | Repeat the glyphs up to the matching `End Loop` N times. Nestable. |
| **End Loop** | Closes the innermost open Loop. |
| **Seal** | Stop speaking. Glyphs **after** the Seal stay in the Incantation for next turn (Seal is consumed). Enables multi-turn setups. |

## 5. Evaluation semantics
```
ctx = { target: Anchor, pendingMods: [], loopStack: [], budget: MaxPayloadExecutions (default 60), timing: EndOfTurn|Invoked }
pc = 0
while pc < glyphs.Count:
    g = glyphs[pc]
    switch g.kind:
      Target   -> ctx.target = g.mode                                  ; pc++
      Modifier -> ctx.pendingMods.Add(g)                               ; pc++
      LoopOpen -> push {start: pc+1, remaining: N × (1+echoes), mods: take(pendingMods)} ; pc++   (≤0 → skip to matching End)
      LoopEnd  -> (pendingMods carry over)
                  if loopStack empty: fizzle(StrayEnd); pc++
                  else if --top.remaining > 0: pc = top.start else pop; pc++
      Seal     -> retain glyphs[pc+1..]; stop
      Payload  -> mods = active loop mods (outer→inner) ++ take(pendingMods)
                  repeat (1 + echoCount):
                      for each payload rune: value = listeners(base) then mods in order; resolve targets; execute
                      if ++payloads > MaxPayloadExecutions: Overload → fizzle remaining; stop
                  pc++
end: unclosed loops implicitly close at the end of the Incantation; leftover pendingMods fizzle.
Also capped at MaxSteps (500) glyph visits so empty nested loops can't spin.
```
Resolution details:
- Payload values ≤ 0 after modifiers are skipped. Numbers are ints.
- Listener adjustments (e.g. Amplify Sigil) apply to the base value **before** modifiers, so Twin doubles them too. Imbue rewrites the base value itself.
- Targets are resolved **at execution time**, so a loop keeps working as enemies die. Dead/untargetable anchor → random.
- If all enemies die, combat ends and evaluation stops (hooks stop firing anyway). Remaining glyphs just vanish.
- If Blood kills the player, the player dies. That's the risk; relics/cards can mitigate.
- `Overload` (budget exhausted): remaining glyphs crack and fizzle. v1 = harmless. Optional balance lever: backlash damage.

### Fizzle table (never throws)
| Situation | Result |
|---|---|
| Modifier with no following payload/loop | Fizzles |
| Target glyph with no later payload | No-op (not shown as fizzle) |
| `End Loop` with no open loop | Fizzles |
| `Loop` never closed | Implicitly closes at end |
| `Loop 0` | Body skipped |
| Payload with no valid target | That payload fizzles; others in the glyph still resolve |
| Budget exceeded | Overload: rest fizzles |
| Unknown/corrupt rune (e.g. from removed content) | Fizzles |

## 6. Example programs
- `[Strike 5]` → 5 damage to anchor.
- `[Loop 3][Strike 4][End]` → 12 damage.
- `[Twin ×2][Loop 3][Strike 14 + Blood 3][End]` → Twin empowers the whole loop: 84 damage, **18 HP loss**. Adding `[Mend 3]` inside the loop gets doubled too (heal 6/iteration) and fully cancels the Blood — placement is the puzzle.
- `[Scatter][Loop 4][Strike 3][End]` → 4 random 3-damage hits.
- `[Loop 2][Strike 3][Defend 3][End]` (Quick Scribe+) → 6 damage and 6 Block. Runic Form wraps the first two runes of every turn like this.
- `[Hex 2][Nova][Strike 6]` → Hex hits the anchor only (Nova comes after), then 9 to the Vulnerable anchor and 6 to the rest.
- `[Strike 8][Seal][Loop 2]` → 8 now; `[Loop 2]` waits at the front of next turn's Incantation.

## 7. Balance principles
- **Delay is almost free** in StS (enemies act after end of turn), so base rune values should sit **below** plain Strike/Defend (Strike rune ~5 vs Strike 6). The upside is composability.
- Rune damage/block is **powered**, because `VulnerablePower`/`WeakPower` only affect powered attacks (`props.IsPoweredAttack()`), and Unpowered runes would make Hex pointless. Consequence: Strength/Dexterity apply per payload execution, exactly like base-game multi-hit attacks — so loops are "multi-hit" and must be costed that way (Strength Rune inside a loop is the obvious watch-list item). Flat scaling comes from **Amplify Sigil** (+N to every scalable payload) and **Imbue**. Fallback lever if Strength loops break balance: make Strike runes Unpowered and have Hex apply bespoke rune-only debuffs.
- Non-scalable Kindle/Swift prevent multiplier abuse; loops still repeat them → keep them rare and consider a per-Incantation cap (lever).
- Loops and Twin are the explosive pieces → Uncommon/Rare, higher cost, or Exhaust.
- Drawbacks (Blood, Mirror) scale with the same modifiers as the upside — self-balancing by design.
- Levers available without redesign: `MaxPayloadExecutions`, Overload backlash, Incantation capacity (future orb-like slots), per-rune caps.

## 8. Architecture (implemented)
```
RunePriestCode/
  Runes/
    Rune.cs               abstract base: Value, Kind, Key (loc key RUNEPRIEST-RUNE_<Key>), Label, HoverTips
    PayloadRunes.cs       PayloadRune (Scalable, Targeting, Resolve) + Strike/Defend/Mend/Strength/Cleanse/Blood/Kindle/Swift/Hex
    ModifierRunes.cs      ModifierRune (Apply, ExtraExecutions) + Amplify/Twin/Echo/Sanctify
    TargetRune.cs         TargetRune(TargetMode)
    FlowRunes.cs          Loop/EndLoop/Seal
    Glyph.cs              runes + Anchor + Source card + Imbued count; Imbue/Empower/Scaled/AsMend transforms; Kind == null means malformed
    RuneBuffer.cs         ordered glyphs, Inscribe/TakeAll/Retain, `event Changed`
    RuneProgram.cs        pure loop-bracket matching
    RuneInterpreter.cs    executes glyphs (loops, modifiers, echo, seal, budget, fizzles, logging)
    RuneContext.cs        per-Speak state + target resolution
    RuneCmd.cs            Inscribe / Speak / SpeakAt / Imbue / ImbueAll / Remove / Transform / Forecast / GetBuffer
    RuneTips.cs           static hover tips (Inscribe, Speak, Imbue, Overflow, Incantation script title)
    IRuneListener.cs      ModifyRuneValue / ModifyLoopCount / ModifyCapacity / KeepsIncantation / ModifyInscription / AfterInscribed / AfterImbued / AfterRemoved / AfterPayload / AfterFizzle / AfterSpeak (+ RuneListeners helper)
    RunePreview.cs        dry-run totals for the Forecast tooltip
  Powers/
    IncantationPower.cs   hosts RuneBuffer (InitInternalData → fresh per clone); Speaks in BeforeSideTurnEnd; DisplayAmount = glyph count; hover tip lists glyphs
    AmplifySigilPower.cs  IRuneListener: +Amount to every scalable payload
    RunicFormPower.cs     ModifyInscription: wraps the first two glyphs each turn in [Loop 2] … [End Loop]
    OddSigilPower.cs      ModifyInscription: every 2nd glyph each turn is Scaled(2)
    TurnCounter.cs        mutable per-turn counter for power InitInternalData
  Cards/
    RuneCard.cs           abstract Glyphs(Creature? anchor); OnPlay → RuneCmd.Inscribe; auto hover tips for Inscribe + runes; Var(name)
    Basic/ Common/ Uncommon/ Rare/
  Relics/ChalkStylus.cs   starter (first Inscribe each turn → 4 Block)
  Nodes/
    NRuneBuffer.cs        row above a player's head; polls RuneCmd.GetBuffer, diffs glyphs by reference, lays out (ReadRightToLeft const)
    NGlyph.cs             one slot; compound runes stacked vertically; appear / pulse (activated) / shake+grey (fizzle) / dissolve
    NRuneSymbol.cs        script character + value label, random bobbing, additive CpuParticles2D scaled by value
    RuneVisuals.cs        style table: family → colour, effect → script, value → character
    RuneFont.cs           composite FontVariation from the game's bundled jpn/kor/tha/rus fonts; HasChar fallback
  Patches/NCreatureRuneBufferPatch.cs  Harmony postfix on NCreature._Ready → attach NRuneBuffer for players
```
Key decisions:
- **Buffer lives on a Power** (`IncantationPower`) on the player creature: auto-receives hooks with a correctly owned `PlayerChoiceContext` (co-op safe), auto-cleans at combat end, and its icon + hover tip is the v1 UI. Applied lazily on first Inscribe; stays for the rest of combat (Amount fixed at 1).
- Evaluate in `BeforeSideTurnEnd`. Relics that inscribe at end of turn must use `BeforeSideTurnEndEarly` (see `ChalkStylus`).
- Strike runes are real attacks via `DamageCmd.Attack(...).FromCard(glyph.Source, null)`; glyphs without a source card (relic/power-inscribed) fall back to `CreatureCmd.Damage` with `ValueProp.Move`.
- Deterministic RNG only (`RunState.Rng.CombatTargets`).
- Rune text lives in `static_hover_tips.json` (`RUNEPRIEST-RUNE_<KEY>.title/.description`). **Not checked by the analyzer** — add entries by hand for every new rune/target mode.
- Card text convention (full rules in `.github/instructions/localization.instructions.md`): one `[gold]Inscribe[/gold] ...` line per sequential glyph; runes of a compound glyph on one line separated by commas (`[blue]Strike[/blue] {Strike:diff()}, [blue]Blood[/blue] {Blood:diff()}`); drawbacks use `inverseDiff()`.
- `RuneCmd.Speak` catches unexpected exceptions and treats them as a total fizzle so a buggy rune can't crash a run.
- Risk to verify: power internal data isn't network-serialized (`NetFullCombatState`) — fine for lockstep play, may desync on co-op reconnect.

### Card boilerplate
```csharp
public sealed class BloodEtching() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar("Strike", 14m, ValueProp.Move), new HpLossVar("Blood", 3m)];

    protected override IEnumerable<Glyph> Glyphs(Creature? anchor) =>
        [Glyph.Of(new StrikeRune(Var("Strike")), new BloodRune(Var("Blood"))).AnchoredTo(anchor)];

    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(4m);
}
```

## 9. Card set (from the design CSVs; values are first-pass, balance pending)
Rune-inscribing damage cards are **Attacks** (with AnyEnemy targeting to set the anchor); others are Skills.
Cards titled "… Rune" use the class suffix `RuneCard` (`HexRuneCard`) so they don't collide with the rune classes.
Rune order follows the CSV except where a single target rune accompanies a Strike (Blind Rage, Chain Lightning,
Execution Rune): there the target rune is inscribed **first** so it governs the card's own Strike.

| Card (class) | Rarity | Type | Cost | Effect | Upgrade |
|---|---|---|---|---|---|
| Strike Rune (`StrikeRuneCard`) | Basic | Attack | 1 | Inscribe [Strike 4]. (×5) | cost 0 |
| Defend Rune (`DefendRuneCard`) | Basic | Skill | 1 | Inscribe [Defend 4]. (×4) | cost 0 |
| Echo Rune (`EchoRuneCard`) | Basic | Skill | 1 | Inscribe [Echo]. (×1) | cost 0 |
| Nova Rune | Common | Skill | 1 | Inscribe [Nova]. | cost 0 |
| Chain Lightning | Common | Attack | 2 | Inscribe [Scatter][Strike 3]×3. | cost 1 |
| Execution Rune | Common | Attack | 1 | Inscribe [Execution][Strike 10]. | Strike 15 |
| Flow State | Common | Skill | 1 | This turn, whenever you Inscribe, draw 1. Exhaust. | no Exhaust |
| Hex Rune | Common | Skill | 1 | Inscribe [Hex 1]. | cost 0 |
| Blind Rage | Common | Attack | 1 | Inscribe [Scatter][Strike 15]. | Strike 20 |
| Quick Jab | Common | Attack | 0 | Deal 3. Inscribe [Strike 3]. | 4 / 4 |
| Mending Rune | Common | Skill | 1 | Inscribe [Mend 2]. Exhaust. | Mend 4 |
| Strength Rune | Common | Skill | 1 | Inscribe [Strength 2]. | cost 0 |
| Runic Barrage | Common | Attack | 1 | Deal 3 damage per rune in the Incantation (one hit each). | 4 |
| Quick Scribe | Common | Attack | 1 | Inscribe [Strike 3][Defend 3]. | wrapped in [Loop 2]…[End Loop] |
| Amplification Rune | Common | Skill | 1 | Inscribe [Amplify +4]. | +6 |
| Blank Rune | Common | Skill | 0 | Imbue 1. Exhaust. | no Exhaust |
| Hasty Scrawl | Common | Attack | 0 | Inscribe [Strike 3][Swift 1]. | Swift 2 |
| Meditate | Common | Skill | 2 | Inscribe [Mend 5][Defend 5]. Exhaust. | cost 1 |
| Amplified Strike | Common | Attack | 1 | Inscribe [Strike 4][Amplify +1]. | 5 / +2 |
| Star Sigil | Uncommon | Power | 1 | Start of turn: Inscribe [Nova]. | also draw 1 on play |
| Nova Slice | Uncommon | Attack | 1 | Inscribe [Nova][Strike 6][Nova]. | Strike 9 |
| Echoing Ward | Uncommon | Skill | 2 (CSV blank) | Inscribe [Defend 8][Echo]. | Defend 11 |
| Loop Rune | Uncommon | Skill | 1 | Inscribe [Loop 2]. | draw 1 |
| Twin Rune | Uncommon | Skill | 1 | Inscribe [Twin ×2]. | draw 1 |
| Kindle Rune | Uncommon | Skill | 1 | Inscribe [Kindle 1]. | cost 0 |
| Warding Sigil | Uncommon | Power | 1 | After Speak: 2 Block per rune Spoken. | 3 |
| Swift Rune | Uncommon | Skill | 1 | Inscribe [Swift 2]. | cost 0 |
| Amplify Sigil | Uncommon | Power | 2 | Every scalable rune +1. | cost 1 |
| Swift Sigil | Uncommon | Power | 1 | Rune removed without being Spoken (Alchemize, fizzle) → draw 1. | cost 0 |
| Alchemize | Uncommon | Skill | 0 | Remove the last rune. Draw 2. | also Imbue 1 |
| Holy Water | Uncommon | Skill | 1 | Inscribe [Cleanse][Mend 4]. Exhaust. | cost 0 |
| Twin Blades | Uncommon | Attack | 2 | Inscribe [Twin ×2][Strike 10]. | Strike 12 |
| Candlelight | Uncommon | Skill | 2 | Inscribe [Kindle N], N = runes in the Incantation. | cost 1 |
| Spellbook | Uncommon | Skill | 1 | Imbue 3. | cost 0 |
| Transmute | Uncommon | Skill | 1 | Imbue 1. Inscribe [Swift 3]. | Imbue 2 |
| Holy Smite | Uncommon | Attack | 1 | Imbue 2. Deal 11 + 5 per rune Imbued. | 14 / 6 |
| Pacify | Uncommon | Skill | 1 | Every payload rune becomes [Mend] of the same total value. Exhaust. | cost 0 |
| Holy Water Sigil | Uncommon | Power | 1 | Healing deals that much damage to a random enemy. | ALL enemies |
| Barrier of Light | Uncommon | Skill | 2 | Until your next turn, damage taken is halved. | cost 1 |
| Paladin Sigil | Uncommon | Power | 2 | Whenever you Imbue a rune, gain 1 Strength. | cost 1 |
| Eternal Sigil | Rare | Power | 3 | Incantation is kept after Speaking. Ethereal. | no Ethereal |
| Runic Form | Rare | Power | 3 | First two runes inscribed each turn are wrapped in [Loop 2]…[End Loop]. | cost 2 |
| Engorged Strike | Rare | Attack | 1 | Inscribe [Strike 8][Amplify +2]. | +3 |
| Blessing | Rare | Skill | 1 | Inscribe [Loop 2][Twin ×2]. Exhaust. | no Exhaust |
| Odd Sigil | Rare | Power | 2 | Every 2nd rune inscribed each turn has its values doubled. | cost 1 |
| Trick of the Light | Rare | Skill | 0 | Inscribe [Swift 3]. Exhaust. | Swift 4 |
| Ancient Tablet | Rare | Skill | 2 | Imbue every rune. | cost 1 |
| Holy Dagger | Rare | Attack | 2 | Deal 1. Gain Dexterity = unblocked damage dealt. | 2 |

Totals: 3 Basic, 16 Common, 21 Uncommon, 8 Rare = 48.

Assumptions made where the CSV was silent (revisit on review): bare "Inscribe Loop/Twin/Kindle/Hex" = Loop 2 / Twin ×2 /
Kindle 1 / Hex 1; bare "Inscribe Swift" = Swift 2; Echoing Ward costs 2; "Draw 1" upgrades draw on play; Imbue = +2 per
rune, newest first; Odd Sigil / Runic Form / Imbued Teacup count per turn; "removed" (Swift Sigil) = left the Incantation
without being Spoken, which includes fizzles; Holy Water Sigil uses the heal amount requested (even at full HP).

| Relic | Rarity | Effect |
|---|---|---|
| Chalk Stylus | Starter | The first time you Inscribe each turn, gain 4 Block. (CSV "starter relic ideas" column) |
| Imbued Teacup | Uncommon | The first rune inscribed each turn has its values doubled (`Glyph.Scaled(2)`, so Loop 2 → Loop 4). |
| Baptised Idol | Uncommon | On pickup, upgrade 2 random cards that Inscribe (`RuneCard`s) — used instead of a "Rune" name match. |
| Runic Sphere | Uncommon | Cards that Inscribe (`RuneCard`s) have Retain (single-turn retain re-applied on draw and at turn start). |
| Enchanted Anvil | Rare | Whenever you Inscribe, deal 2 damage to ALL enemies. |
| Midas Hand | Rare | Whenever you Inscribe, gain 2 Gold. |

| Potion | Rarity | Effect |
|---|---|---|
| Echo Brew | Common | Inscribe [Echo]. |
| Lingering Aroma | Rare | This turn the Incantation is kept after Speaking (`LingeringAromaPower`, removed at your next turn start). |
| Thrumming Elixir | Rare | Every scalable rune already inscribed gains +2 (`Glyph.Empower`; not a modifier, so loops don't multiply it). |

Balance watch-list: Blind Rage (15 for 1), Strength Rune in loops, Imbued Teacup + Runic Form (doubles the Loop),
Odd Sigil + Blessing, Eternal Sigil + loops (bounded by the 60-payload Overload), Holy Water Sigil + Nova Mend.

## 10. Roadmap
1. ~~**Phase 0 – Scaffolding**~~ ✔ starter deck/relic replaced, hover tips.
2. ~~**Phase 1 – Rune core**~~ ✔ all v1 runes, power-hover UI, `[Rune]` logging.
3. ~~**Phase 2 – Cards**~~ ✔ prototype card set (since replaced), Chalk Stylus, localization.
4. ~~**Phase 3 – Overhead UI**~~ ✔ see §12.
5. ~~**Phase 4 – Expansion**~~ ✔ see §13 (engine features; the prototype content was replaced in Phase 5).
6. ~~**Phase 5 – Designed card set**~~ ✔ §9: the 48 cards / 5 relics / 3 potions from the design CSVs, Imbue, Hex/Strength/Cleanse runes. Remaining: in-game playtesting + balance pass, real art, card-level damage preview, co-op testing.

## 11. Decisions log
- Names Incantation / Glyph / Inscribe / Speak / Fizzle — kept (no objection).
- Strike runes count as **normal attacks** (Strength, Vulnerable, Weak apply per hit).
- Default target is **Anchor**; supports enemies for offensive runes and allies/self for supportive ones.
- **Mend** is in v1 with very low values.
- Incantation **clears every turn**; **Seal** is the retention mechanism.
- Playtest pass 1: renamed Ward→Block, Insight→Soul, Hex→Weakening, Amplify→Add, Twin→Multiply, Seek→Chaos, Cull→Execution; removed Chain (and Chain Sigil, Chain Lightning, Cascade); buffer reads left to right; starter deck 5 Etch / 4 Warding Rune / 1 Echo Sign.
- Playtest pass 2: player-facing text says "rune" only (no "glyph"); card text uses one Inscribe line per sequential rune and commas for compound runes; Etch → Strike Rune, Warding Rune → Defend Rune, Mending Glyph → Mending Rune, Glyph Guard → Rune Guard, Smudged/Stray Glyph → Smudged/Stray Rune, Glyph Draught → Rune Draught.
- **Concentration** keyword (`RunePriestKeywords.Concentration`, card keyword shown before the text): the granted power overrides `RunePriestPower.Concentration => true` and is removed the moment its owner loses HP (`AfterCurrentHpChanged`, delta < 0 — Blood runes count). First user was Eternal Script; no current card uses it (Eternal Sigil is Ethereal instead), the keyword stays available.
- Design pass 3 (CSV card set): renamed back Add→**Amplify**, Multiply→**Twin**, Soul→**Swift**, Chaos→**Scatter**, Block→**Defend** (rune; avoids `[blue]Block[/blue]` vs `[gold]Block[/gold]`), Weakening→**Hex** (now Weak + Vulnerable). Removed Expose/Venom (folded into Hex). Added **Strength**, **Cleanse**, and **Imbue** (instant +2 to inscribed runes). All prototype cards/relics/potions and the Smudged/Stray Rune status/curse were deleted; Blood/Sanctify/Seal/Mirror remain engine-only. Chalk Stylus became "first Inscribe each turn → 4 Block".

## 12. Overhead visuals (Phase 3)
From the user's sketch: runes float in a row over the head, **read left to right** (first glyph spoken is leftmost;
flip with `NRuneBuffer.ReadRightToLeft`). Compound glyphs stack vertically = "these resolve together".

- **Colour = family**: Offense red (Strike/Hex), Support green (Defend/Mend/Strength/Cleanse), Resource gold (Kindle/Swift),
  Cost crimson (Blood), Modifier violet, Target pink, Flow pale blue.
- **Shape = effect, character = value** (higher value → later/denser character):

| Rune | Script | Characters |
|---|---|---|
| Strike | Kanji by stroke count (step 3) | 刀刃斤矛伐戒刺剣殺斬裂戦撃闘轟 |
| Defend | Greek (step 2) | αβγ…ωΩ |
| Mend | Hangul | 가나다…하 |
| Blood | Cyrillic | БГДЖ…Я |
| Kindle | Thai | กขค… |
| Swift | Katakana | アイウ… |
| Strength | Hiragana | あいう… |
| Hex | Bopomofo | ㄅㄆㄇ… |
| Cleanse | reference mark | ※ |
| Amplify / Twin / Echo / Sanctify | math | ⊕ ⊗ 〃 ⊘ |
| Anchor / Scatter / Nova / Execution / Mirror | symbols | ◎ ∴ ☆ ▽ ◇ |
| Loop / End Loop / Seal | CJK brackets / seal mark | 〔 〕 〆 |

- Small numeric value label on valued runes (hover the Incantation power for full text).
- Motion: per-symbol random sine bob (cosmetic `GD.Randf`, not run RNG). Particles: additive `CpuParticles2D`, count/speed/size scale with value.
- Speak feedback: interpreter raises `RuneBuffer.GlyphActivated` / `GlyphFizzled` / `SpeakEnded`, with a short
  `Cmd.CustomScaledWait` per glyph. Activated glyphs pulse + burst then stay dimmed ("spent"); fizzles shake and grey;
  at the end spent glyphs dissolve upward in sequence while Seal-retained glyphs stay.
- Fonts: only the game's bundled locale fonts are used; missing glyphs log a warning and render `◆`.
  To use scripts not covered (e.g. Elder Futhark runes ᚠᚢᚦ), ship an OFL font such as Noto Sans Runic in `RunePriest/`.
- Known gaps: no mouse hover on the floating runes yet (use the Incantation power tooltip); placeholder particles are untextured squares.

## 13. Phase 4 — Expansion
Decisions made autonomously (user unavailable; revisit on review):
- **Capacity is opt-in via content.** Default stays unlimited. `IRuneListener.ModifyCapacity` (smallest wins). When a glyph
  would exceed capacity it **Overflows**: the oldest glyph is Spoken alone, immediately (`RuneCmd.SpeakAt`), like Defect evoke.
  UI draws faint empty-slot rings when a capacity is active. No current content sets a capacity.
- **Curse runes** (Mirror, Blood) are engine-only since Phase 5; the Smudged/Stray Rune status/curse cards were removed with
  the prototype set. Re-add via `[Pool(typeof(StatusCardPool))]` + `AfterCardDrawn` if wanted.
- **Forecast**: the Incantation power tooltip shows a dry run (`RuneCmd.Forecast` → interpreter with `RunePreview`): totals
  per payload, fizzles, overload. Base values only; preview never resolves targets so it can't advance the combat RNG.
- New listener hooks: `ModifyLoopCount`, `ModifyCapacity`, `KeepsIncantation`, `AfterInscribed`, `AfterPayload`,
  `AfterFizzle`, `AfterSpeak` (with `RuneContext.GlyphsSpoken` / `Fizzles`). Value/loop/capacity hooks must be pure.
- New rune: **Sanctify** (modifier, ⊘). (Venom was added here and removed again in Phase 5.)
- New speak variants: `Speak(..., keep: true)`, `SpeakAt(index)` (Overflow).
- Phase 5 hooks: `ModifyInscription` (reshape a cast before it lands — Runic Form, Odd Sigil, Imbued Teacup), `AfterImbued`
  (Paladin Sigil), `AfterRemoved` (Swift Sigil); commands `RuneCmd.Imbue/ImbueAll/Remove/Transform`.

The Phase 4 prototype content (Chalk Line, Tight Script, Slate Tablet, Smudged/Stray Rune, …) was **removed** in Phase 5 in
favour of the designed set in §9. The engine features above (capacity/Overflow, curse runes, Forecast, listener hooks,
keep/SpeakAt) are still available for future content.
