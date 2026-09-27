# Rune System Design — "The Incantation"

Status: **v0.4 — Phases 0–4 implemented** (rune core, 73 cards, 9 relics, 3 potions, overhead UI, capacity, forecast).
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
| **Rune** | Atomic instruction: `Strike 5`, `Loop ×2`, `Chaos`. | `Rune` |
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
| **Block X** | Gain X Block. | ✔ | Powered (`BlockProps.card`) so Dexterity/Frail apply. Block at end of turn still protects during enemy turn. |
| **Mend X** | Heal X HP. | ✔ | Strong in StS — keep rare, low values, often Exhaust. |
| **Blood X** | Lose X HP (unblockable, self). | ✔ | The drawback half of compound glyphs. Scales with modifiers — that's the point. |
| **Kindle X** | Gain X Energy. | ✘ | End-of-turn → `EnergyNextTurnPower`. Invoked → immediate. |
| **Soul X** | Draw X cards. | ✘ | End-of-turn → `DrawCardsNextTurnPower`. Invoked → immediate. |
| **Weakening X** | Apply X Weak to target(s). | ✔ | |
| **Expose X** | Apply X Vulnerable to target(s). | ✔ | Order matters: Expose before Strike within an Incantation amplifies later hits. |
| **Venom X** | Apply X Poison to target(s). | ✔ | Phase 4. |

¹ *Scalable* payloads are affected by Add/Multiply. Non-scalable ones still repeat in loops.

### Modifiers (apply to the **next glyph**)
| Rune | Effect |
|---|---|
| **Add +X** | +X to every scalable payload in the next glyph. |
| **Multiply ×N** | Multiply every scalable payload in the next glyph by N. |
| **Echo** | Execute the next glyph one extra time. |
| **Sanctify** | Blood runes in the next glyph (or whole loop body) resolve as 0. Phase 4. |

Rules:
- Pending modifiers **stack and apply in inscription order**: `Add 3, Multiply ×2, Strike 5` → (5+3)×2 = 16; `Multiply ×2, Add 3, Strike 5` → 5×2+3 = 13. Ordering *is* the skill expression.
- A modifier applies to **all** scalable payloads in the glyph — including `Blood`. Doubling `Strike 14 + Blood 3` doubles both.
- **Target glyphs are transparent**: modifiers pass through them to the following glyph.
- If the next glyph is a **Loop**, the modifier applies to **the entire loop body on every iteration**.
- A modifier with nothing valid after it (end of Incantation, `Seal`) **fizzles**.
- Loop boundaries are transparent: a modifier at the end of a loop body **rolls over** to the first glyph of the next iteration (`[Loop 3][Strike 6][Echo]` → iterations 2 and 3 strike twice), and after the last iteration to the glyph after the loop. Target modes already persist across iterations.

### Targets (persist until changed)
Each payload rune declares a `RuneTargeting`: **Enemy** (Strike, Weakening, Expose), **Ally** (Block, Mend), or **Self**
(Blood, Kindle, Soul — always the caster, ignores target mode). The mode picks from the relevant side:

| Rune | Enemy payloads | Ally payloads |
|---|---|---|
| *(default)* **Anchor** | The enemy targeted when the glyph's card was played; if dead/none → random enemy. | The ally targeted by the card; if none → the caster. |
| **Chaos** | Random enemy, re-rolled **per execution** (loops scatter). | Random ally. |
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
- Listener adjustments (e.g. Resonance) apply to the base value **before** modifiers, so Multiply doubles Resonance too.
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
- `[Multiply ×2][Loop 3][Strike 14 + Blood 3][End]` → Multiply empowers the whole loop: 84 damage, **18 HP loss**. Adding `[Mend 3]` inside the loop gets doubled too (heal 6/iteration) and fully cancels the Blood — placement is the puzzle.
- `[Chaos][Loop 4][Strike 3][End]` → 4 random 3-damage hits.
- `[Expose 2][Nova][Strike 6]` → Expose hits the anchor only (Nova comes after), then 9 to the Vulnerable anchor and 6 to the rest.
- `[Strike 8][Seal][Loop 2]` → 8 now; `[Loop 2]` waits at the front of next turn's Incantation.

## 7. Balance principles
- **Delay is almost free** in StS (enemies act after end of turn), so base rune values should sit **below** plain Strike/Defend (Strike rune ~5 vs Strike 6). The upside is composability.
- Rune damage/block is **powered**, because `VulnerablePower`/`WeakPower` only affect powered attacks (`props.IsPoweredAttack()`), and Unpowered runes would make Expose/Weakening pointless. Consequence: Strength/Dexterity apply per payload execution, exactly like base-game multi-hit attacks — so loops are "multi-hit" and must be costed that way. The character's own scaling stat is **Resonance** (power): +N to every Strike and Block payload, analogous to Defect Focus. Fallback lever if Strength loops break balance: make Strike runes Unpowered and have Expose/Weakening apply bespoke rune-only debuffs.
- Non-scalable Kindle/Soul prevent multiplier abuse; loops still repeat them → keep them rare, compound with Blood, and consider a per-Incantation cap (lever).
- Loops and Multiply are the explosive pieces → Uncommon/Rare, higher cost, or Exhaust.
- Drawbacks (Blood, Mirror) scale with the same modifiers as the upside — self-balancing by design.
- Levers available without redesign: `MaxPayloadExecutions`, Overload backlash, Incantation capacity (future orb-like slots), per-rune caps.

## 8. Architecture (implemented)
```
RunePriestCode/
  Runes/
    Rune.cs               abstract base: Value, Kind, Key (loc key RUNEPRIEST-RUNE_<Key>), Label, HoverTips
    PayloadRunes.cs       PayloadRune (Scalable, Targeting, Resolve) + Strike/Block/Mend/Blood/Kindle/Soul/Weakening/Expose
    ModifierRunes.cs      ModifierRune (Apply, ExtraExecutions) + Add/Multiply/Echo
    TargetRune.cs         TargetRune(TargetMode)
    FlowRunes.cs          Loop/EndLoop/Seal
    Glyph.cs              runes + Anchor + Source card; Kind == null means malformed
    RuneBuffer.cs         ordered glyphs, Inscribe/TakeAll/Retain, `event Changed`
    RuneProgram.cs        pure loop-bracket matching
    RuneInterpreter.cs    executes glyphs (loops, modifiers, echo, seal, budget, fizzles, logging)
    RuneContext.cs        per-Speak state + target resolution
    RuneCmd.cs            Inscribe(ctx, player, glyphs, source) / Speak(ctx, player, timing) / GetBuffer(creature)
    RuneTips.cs           static hover tips (Inscribe, Speak, Incantation script title)
    IRuneListener.cs      ModifyRuneValue / ModifyLoopCount / ModifyCapacity / KeepsIncantation / AfterInscribed / AfterPayload / AfterFizzle / AfterSpeak (+ RuneListeners helper)
    RunePreview.cs        dry-run totals for the Forecast tooltip
  Powers/
    IncantationPower.cs   hosts RuneBuffer (InitInternalData → fresh per clone); Speaks in BeforeSideTurnEnd; DisplayAmount = glyph count; hover tip lists glyphs
    ResonancePower.cs     IRuneListener: +Amount to Strike/Block
    ScriptoriumPower.cs   AfterPlayerTurnStart → Inscribe Strike(Amount)
  Cards/
    RuneCard.cs           abstract Glyphs(Creature? anchor); OnPlay → RuneCmd.Inscribe; auto hover tips for Inscribe + runes; Var(name)
    Basic/ Common/ Uncommon/ Rare/
  Relics/ChalkStylus.cs   starter
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

## 9. Card set (implemented; values are first-pass, balance pending)
Rune-inscribing damage cards are **Attacks** (with AnyEnemy targeting to set the anchor); others are Skills.

| Card (class) | Rarity | Type | Cost | Effect | Upgrade |
|---|---|---|---|---|---|
| Strike (`StrikeRunePriest`) | Basic | Attack | 1 | Deal 6 damage. (×4) | +3 |
| Defend (`DefendRunePriest`) | Basic | Skill | 1 | Gain 5 Block. (×4) | +3 |
| Strike Rune (`StrikeRuneCard`) | Basic | Attack | 1 | Inscribe [Strike 6]. (×5) | +3 |
| Defend Rune (`DefendRuneCard`) | Basic | Skill | 1 | Inscribe [Block 5]. (×4) | +3 |
| Echo Sign | Basic | Skill | 1 | Inscribe [Echo]. | cost 0 |
| Blood Etching | Common | Attack | 1 | Inscribe [Strike 14 + Blood 3]. | Strike +4 |
| Scatter Marks | Common | Attack | 1 | Inscribe [Chaos][Strike 4][Strike 4]. | +2 each |
| Nova Sigil | Common | Skill | 0 | Inscribe [Nova]. | Retain |
| Quick Carve | Common | Attack | 0 | Deal 3 damage. Inscribe [Strike 3]. | +1 / +1 |
| Hexing Mark | Common | Skill | 1 | Inscribe [Weakening 1 + Expose 1]. | +1 / +1 |
| Open Circle | Uncommon | Skill | 1 | Inscribe [Loop 2]. | cost 0 |
| Close Circle | Uncommon | Skill | 0 | Inscribe [End Loop]. Draw 1. | Draw +1 |
| Amplifying Rune | Uncommon | Skill | 1 | Inscribe [Add +4]. | +2 |
| Invoke | Uncommon | Skill | 1 | Speak your Incantation now. Exhaust. | cost 0 |
| Kindling Mark | Uncommon | Skill | 0 | Inscribe [Kindle 1 + Blood 2]. | Blood −1 |
| Mending Rune | Uncommon | Skill | 1 | Inscribe [Mend 2]. Exhaust. | +1 |
| Resonance | Uncommon | Power | 1 | Gain 1 Resonance. | +1 |
| Grand Circle | Rare | Skill | 2 | Inscribe [Loop 3]. | +1 |
| Twin Sigil | Rare | Skill | 1 | Inscribe [Multiply ×2]. | cost 0 |
| Seal of Patience | Rare | Skill | 1 | Inscribe [Seal]. Retain. | cost 0 |
| Blood Covenant | Rare | Attack | 2 | Inscribe [Strike 30 + Blood 8]. | Strike +8 |
| Scriptorium | Rare | Power | 2 | At the start of your turn, Inscribe [Strike 4]. | +2 |

Starter relic: **Chalk Stylus** — "At the end of your turn, if your Incantation is empty, Inscribe [Block 4]."
Relic pool and potions: see §13.

## 10. Roadmap
1. ~~**Phase 0 – Scaffolding**~~ ✔ starter deck/relic replaced, hover tips.
2. ~~**Phase 1 – Rune core**~~ ✔ all v1 runes (incl. Execution/Mirror, Echo/Seal, Kindle/Soul/Weakening/Expose), power-hover UI, `[Rune]` logging.
3. ~~**Phase 2 – Cards**~~ ✔ §9 list, Chalk Stylus, localization.
4. ~~**Phase 3 – Overhead UI**~~ ✔ see §12.
5. ~~**Phase 4 – Expansion**~~ ✔ see §13. Remaining: in-game playtesting + balance pass, real art, card-level damage preview, co-op testing.

## 11. Decisions log
- Names Incantation / Glyph / Inscribe / Speak / Fizzle — kept (no objection).
- Strike runes count as **normal attacks** (Strength, Vulnerable, Weak apply per hit).
- Default target is **Anchor**; supports enemies for offensive runes and allies/self for supportive ones.
- **Mend** is in v1 with very low values.
- Incantation **clears every turn**; **Seal** is the retention mechanism.
- Playtest pass 1: renamed Ward→Block, Insight→Soul, Hex→Weakening, Amplify→Add, Twin→Multiply, Seek→Chaos, Cull→Execution; removed Chain (and Chain Sigil, Chain Lightning, Cascade); buffer reads left to right; starter deck 5 Etch / 4 Warding Rune / 1 Echo Sign.
- Playtest pass 2: player-facing text says "rune" only (no "glyph"); card text uses one Inscribe line per sequential rune and commas for compound runes; Etch → Strike Rune, Warding Rune → Defend Rune, Mending Glyph → Mending Rune, Glyph Guard → Rune Guard, Smudged/Stray Glyph → Smudged/Stray Rune, Glyph Draught → Rune Draught.

## 12. Overhead visuals (Phase 3)
From the user's sketch: runes float in a row over the head, **read left to right** (first glyph spoken is leftmost;
flip with `NRuneBuffer.ReadRightToLeft`). Compound glyphs stack vertically = "these resolve together".

- **Colour = family**: Offense red (Strike/Weakening/Expose), Support green (Block/Mend), Resource gold (Kindle/Soul),
  Cost crimson (Blood), Modifier violet, Target pink, Flow pale blue.
- **Shape = effect, character = value** (higher value → later/denser character):

| Rune | Script | Characters |
|---|---|---|
| Strike | Kanji by stroke count (step 3) | 刀刃斤矛伐戒刺剣殺斬裂戦撃闘轟 |
| Block | Greek (step 2) | αβγ…ωΩ |
| Mend | Hangul | 가나다…하 |
| Blood | Cyrillic | БГДЖ…Я |
| Kindle | Thai | กขค… |
| Soul | Katakana | アイウ… |
| Weakening | Hiragana | あいう… |
| Expose | Bopomofo | ㄅㄆㄇ… |
| Add / Multiply / Echo | math | ⊕ ⊗ 〃 |
| Anchor / Chaos / Nova / Execution / Mirror | symbols | ◎ ∴ ☆ ▽ ◇ |
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
  UI draws faint empty-slot rings when a capacity is active. Sources: Tight Script (power, 4), Slate Tablet (relic, 5).
- **Curse-rune sources are our own cards.** Status **Smudged Rune** (in `StatusCardPool`; Unplayable, Ethereal; when drawn
  Inscribe Mirror) comes from Hasty Scrawl / Wild Scrawl. Curse **Stray Rune** (in `CurseCardPool`; when drawn Inscribe
  Blood 2) comes from the Cursed Quill relic. Counterplay: Steady Hand (Inscribe Anchor), Erase (remove first glyph).
- **Forecast**: the Incantation power tooltip shows a dry run (`RuneCmd.Forecast` → interpreter with `RunePreview`): totals
  per payload, fizzles, overload. Base values only; preview never resolves targets so it can't advance the combat RNG.
- New listener hooks: `ModifyLoopCount`, `ModifyCapacity`, `KeepsIncantation`, `AfterInscribed`, `AfterPayload`,
  `AfterFizzle`, `AfterSpeak` (with `RuneContext.GlyphsSpoken` / `Fizzles`). Value/loop/capacity hooks must be pure.
- New runes: **Venom** (Poison, circled katakana ㋐…) and **Sanctify** (modifier, ⊘).
- New speak variants: `Speak(..., keep: true)` (Rehearse, Eternal Script), `SpeakAt(index)` (Utter, Overflow).

Content added (all placeholder art):

| Rarity | Cards |
|---|---|
| Common (+13) | Chalk Line, Rune Slash, Steady Hand, Culling Mark, Venom Etching, Warding Circle, Hasty Scrawl, Glimpse, Sanguine Ward, Etched Guard, Double Stroke, Hex Bolt, Reinscribe |
| Uncommon (+22) | Blood Ritual, Venomous Circle, Lifeline, Wild Scrawl, Recite, Utter, Resounding Sign, Grand Ward, Quickening, Warding Litany (P), Chalk Dust (P), Echoing Hymn (P), Sigil of Seeking, Cull the Weak, Deep Ink, Hex Circle, Rune Barrage, Etched Shield, Honing Stroke, Erase, Tight Script (P), Rune Guard |
| Rare (+11) | Sanctify Sigil, Infinite Circle, Rehearse, Eternal Script (P), Blood Sonnet, Nova Burst, Ink Covenant (P), Arcane Flow (P), Final Word, Palimpsest, Word of Power |
| Status / Curse | Smudged Rune, Stray Rune |

Totals: 5 Basic, 18 Common, 29 Uncommon, 16 Rare (+2 status/curse) = 70.

| Relic | Rarity | Effect |
|---|---|---|
| Chalk Stylus | Starter | End of turn, if Incantation empty, Inscribe Block 4. |
| Whetstone Rune | Common | Strike runes +1. |
| Warding Charm | Common | Start of combat, Inscribe Block 6. |
| Blood Chalice | Uncommon | Blood runes −1. |
| Ink Pot | Uncommon | Speak 5+ glyphs at once → +1 Energy (next turn if end of turn). |
| Ouroboros | Rare | Loops repeat +1 time. |
| Cursed Quill | Rare | Strike/Block runes +2; adds a Stray Rune curse on pickup. |
| Slate Tablet | Rare | +1 max Energy; capacity 5 (Overflow). |
| Tuning Fork | Shop | Start of combat, gain 1 Resonance. |

| Potion | Rarity | Effect |
|---|---|---|
| Liquid Ink | Common | Inscribe Add +5. |
| Rune Draught | Uncommon | Speak your Incantation now. |
| Loop Tonic | Rare | Inscribe Loop 3. |

Balance watch-list for playtesting: Eternal Script + loops (bounded by the 60-payload Overload), Palimpsest doubling,
Sanctify before a Blood loop, Arcane Flow scaling, Ink Covenant + Blood Sonnet, Slate Tablet energy vs. capacity.
