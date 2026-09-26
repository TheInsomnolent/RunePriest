# Rune System Design — "The Incantation"

Status: **Proposal v0.1** (not yet implemented). Update this doc when decisions change.

## 1. Pitch
Noita lets you build wands from spell tiles that act as a block-coding language. The Rune Priest reframes that for
StS2: cards **Inscribe** glyphs into an ordered buffer (the **Incantation**) floating above the character. At end of
turn the Incantation is **Spoken** — evaluated left→right as a tiny program — then cleared. Cards are cheap building
blocks; the power comes from *ordering* them. Malformed programs never crash: bad pieces **Fizzle** with a puff of
chalk dust and the rest keeps going.

## 2. Terminology
| Term | Meaning | Code |
|---|---|---|
| **Rune** | Atomic instruction: `Strike 5`, `Loop ×2`, `Seek`. | `Rune` |
| **Glyph** | What one card inscribes into one slot. Usually 1 rune; **compound** glyphs bundle several payload runes (e.g. `Strike 14 + Blood 3`). A card may inscribe several glyphs. | `Glyph` |
| **Incantation** | The ordered glyph buffer above the head. Unlimited length in v1. | `RuneBuffer` |
| **Inscribe** | Append glyph(s) to the Incantation (card keyword). | `RuneCmd.Inscribe` |
| **Speak** | Evaluate the Incantation. Happens automatically at end of turn; **Invoke** cards speak it mid-turn. | `RuneCmd.Speak` |
| **Fizzle** | A glyph that can't do anything dissolves harmlessly. | `FizzleReason` |

## 3. Rune categories
Every glyph has exactly one **kind**, determined by its runes:

1. **Payload glyph** — one or more payload runes (effects). The only kind that does something directly.
2. **Modifier glyph** — alters the *next* glyph.
3. **Target glyph** — sets the targeting mode for all later payloads.
4. **Flow glyph** — control flow (loops, seal).

Compound glyphs may only combine *payload* runes (optionally with a baked-in anchor target). Modifier/Target/Flow runes
are always their own glyph (a card can inscribe several glyphs, e.g. `[Loop 2][Strike 4][End]`). This keeps the
grammar trivially parseable and the visuals readable.

## 4. Base rune set (v1)

### Payloads
| Rune | Effect | Scalable¹ | Notes |
|---|---|---|---|
| **Strike X** | Deal X damage to the current target(s). | ✔ | Powered attack (`ValueProp.Move`) so Vulnerable/Weak/Strength apply per hit, like multi-hit attacks (see §7). |
| **Ward X** | Gain X Block. | ✔ | Powered (`BlockProps.card`) so Dexterity/Frail apply. Block at end of turn still protects during enemy turn. |
| **Mend X** | Heal X HP. | ✔ | Strong in StS — keep rare, low values, often Exhaust. |
| **Blood X** | Lose X HP (unblockable, self). | ✔ | The drawback half of compound glyphs. Scales with modifiers — that's the point. |
| **Kindle X** | Gain X Energy. | ✘ | End-of-turn → `EnergyNextTurnPower`. Invoked → immediate. |
| **Insight X** | Draw X cards. | ✘ | End-of-turn → `DrawCardsNextTurnPower`. Invoked → immediate. |
| **Hex X** | Apply X Weak to target(s). | ✔ | |
| **Expose X** | Apply X Vulnerable to target(s). | ✔ | Order matters: Expose before Strike within an Incantation amplifies later hits. |

¹ *Scalable* payloads are affected by Amplify/Multiply. Non-scalable ones still repeat in loops.

### Modifiers (apply to the **next glyph**)
| Rune | Effect |
|---|---|
| **Amplify +X** | +X to every scalable payload in the next glyph. |
| **Twin ×N** | Multiply every scalable payload in the next glyph by N. |
| **Echo** | Execute the next glyph one extra time. |

Rules:
- Pending modifiers **stack and apply in inscription order**: `Amplify 3, Twin ×2, Strike 5` → (5+3)×2 = 16; `Twin ×2, Amplify 3, Strike 5` → 5×2+3 = 13. Ordering *is* the skill expression.
- A modifier applies to **all** scalable payloads in the glyph — including `Blood`. Doubling `Strike 14 + Blood 3` doubles both.
- **Target glyphs are transparent**: modifiers pass through them to the following glyph.
- If the next glyph is a **Loop**, the modifier applies to **the entire loop body on every iteration**.
- A modifier with nothing valid after it (end of Incantation, `End Loop`, `Seal`) **fizzles**.

### Targets (persist until changed)
| Rune | Damage/debuff payloads hit… |
|---|---|
| *(default)* **Anchor** | The enemy targeted when that glyph's card was played; if dead/none → random living enemy. |
| **Seek** | A random living enemy, re-rolled **per payload execution** (loops scatter). |
| **Nova** | All enemies. |
| **Chain** | Enemies in order, advancing one per payload execution (wraps). |
| **Cull** | The living enemy with the lowest current HP. |
| **Mirror** *(curse rune)* | **Yourself.** Only from curses/status cards/enemy effects — a negative rune to plan around. |

Self-payloads (Ward, Mend, Blood, Kindle, Insight) always affect the caster regardless of target mode.

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
      LoopOpen -> push {start: pc+1, remaining: N, mods: take(pendingMods)} ; pc++   (N<=0 → skip to matching End)
      LoopEnd  -> if loopStack empty: fizzle(StrayEnd); pc++
                  else if --top.remaining > 0: pc = top.start else pop; pc++
      Seal     -> retain glyphs[pc+1..]; stop
      Payload  -> mods = take(pendingMods) ++ active loop mods
                  repeat (1 + echoCount):
                      for each payload rune: value = apply(mods, base) + listeners; resolve targets; execute
                      if --budget == 0: Overload → fizzle remaining; stop
                  pc++
end: unclosed loops implicitly close at the end of the Incantation; leftover pendingMods fizzle.
```
Resolution details:
- Payload values are clamped `≥ 0` after modifiers. Numbers are ints (round down).
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
- `[Seek][Loop 4][Strike 3][End]` → 4 random 3-damage hits.
- `[Expose 2][Nova][Strike 6]` → Expose hits the anchor only (Nova comes after), then 9 to the Vulnerable anchor and 6 to the rest.
- `[Strike 8][Seal][Loop 2]` → 8 now; `[Loop 2]` waits at the front of next turn's Incantation.

## 7. Balance principles
- **Delay is almost free** in StS (enemies act after end of turn), so base rune values should sit **below** plain Strike/Defend (Strike rune ~5 vs Strike 6). The upside is composability.
- Rune damage/block is **powered**, because `VulnerablePower`/`WeakPower` only affect powered attacks (`props.IsPoweredAttack()`), and Unpowered runes would make Expose/Hex pointless. Consequence: Strength/Dexterity apply per payload execution, exactly like base-game multi-hit attacks — so loops are "multi-hit" and must be costed that way. The character's own scaling stat is **Resonance** (power): +N to every Strike and Ward payload, analogous to Defect Focus. Fallback lever if Strength loops break balance: make Strike runes Unpowered and have Expose/Hex apply bespoke rune-only debuffs.
- Non-scalable Kindle/Insight prevent multiplier abuse; loops still repeat them → keep them rare, compound with Blood, and consider a per-Incantation cap (lever).
- Loops and Twin are the explosive pieces → Uncommon/Rare, higher cost, or Exhaust.
- Drawbacks (Blood, Mirror) scale with the same modifiers as the upside — self-balancing by design.
- Levers available without redesign: `MaxPayloadExecutions`, Overload backlash, Incantation capacity (future orb-like slots), per-rune caps.

## 8. Architecture
```
RunePriestCode/
  Runes/
    Rune.cs               abstract base: Id, Kind, BaseValue, Scalable, Clone(), HoverTip, IconPath
    Payloads/*.cs         StrikeRune, WardRune, MendRune, BloodRune, KindleRune, InsightRune, HexRune, ExposeRune
    Modifiers/*.cs        AmplifyRune, TwinRune, EchoRune
    Targets/*.cs          TargetRune(TargetMode) — Anchor/Seek/Nova/Chain/Cull/Mirror
    Flow/*.cs             LoopRune, EndLoopRune, SealRune
    Glyph.cs              IReadOnlyList<Rune> + Kind + Anchor (Creature?) + SourceCard
    RuneBuffer.cs         ordered glyph list, Inscribe/Clear/RetainFrom, `event Changed`
    RuneProgram.cs        PURE "compile": bracket matching, modifier binding, stray/implicit fixes → execution plan (unit-testable, no game refs)
    RuneInterpreter.cs    executes a plan step-by-step against combat (targets resolved live), budget, fizzles, events for UI animation
    RuneContext.cs        target mode, chain index, timing, budget, owner, choice context
    RuneCmd.cs            Inscribe(ctx, player, glyphs, sourceCard) / Speak(ctx, player, timing) / Clear(player)
    IRuneListener.cs      OnInscribed, BeforeSpeak, ModifyPayloadValue, AfterPayload, OnFizzle, AfterSpeak — implemented by our powers/relics
  Powers/
    IncantationPower.cs   holds the RuneBuffer on the player creature; BeforeSideTurnEnd(side==Player) → RuneCmd.Speak. Amount = glyph count; hover tip lists glyphs (v1 UI).
    ResonancePower.cs
  Cards/
    RuneCard.cs           base for rune cards: abstract IEnumerable<Glyph> Inscribe(CardPlay play); OnPlay → RuneCmd.Inscribe; helpers Glyph.Of(...), rune value from DynamicVars
  Nodes/
    NRuneBuffer.cs        Godot Control above the head; subscribes to RuneBuffer.Changed + interpreter step events; highlights executing glyph; fizzle VFX
  Patches/
    NCreatureRuneBufferPatch.cs  Harmony postfix on NCreature._Ready → attach NRuneBuffer for players
```
Key decisions:
- **Buffer lives on a Power** (`IncantationPower`) on the player creature: auto-receives hooks with a correctly owned `PlayerChoiceContext` (co-op safe), auto-cleans at combat end, and its icon + hover tip is a free v1 UI before the Godot node exists. Alternative considered: `SpireField<Player,RuneBuffer>` + `CustomSingletonModel` — rejected for ownership/co-op reasons.
- **Compile/execute split**: `RuneProgram` is pure C# → can get a small xUnit test project later without loading the game.
- Evaluate in `BeforeSideTurnEnd` (before hand discard, after most "end of turn" relic early hooks).
- Deterministic RNG only (`RunState.Rng.CombatTargets`).
- Rune text lives in `static_hover_tips.json` (`RUNEPRIEST-RUNE_STRIKE.title/.description`), built into `HoverTip`s.
- Custom card keyword **Inscribe** via BaseLib `[CustomEnum]` for tooltips; "Incantation", "Speak", "Fizzle" as static hover tips.
- Risk to verify: power custom fields aren't network-serialized (`NetFullCombatState`) — fine for lockstep play, may desync on co-op reconnect.

### Card boilerplate sketch
```csharp
public sealed class BloodEtching() : RuneCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("Strike", 14m), new("Blood", 3m)];
    protected override IEnumerable<Glyph> Inscribe(CardPlay play) =>
        [Glyph.Of(Rune.Strike(Var("Strike")), Rune.Blood(Var("Blood"))).AnchoredTo(play.Target)];
    protected override void OnUpgrade() => DynamicVars["Strike"].UpgradeValueBy(4m);
}
```

## 9. Initial card set (meets character minimums: every rarity + Attack/Skill/Power)
Rune-inscribing damage cards are **Attacks** (with AnyEnemy targeting to set the anchor); others are Skills.

| Card | Rarity | Type | Cost | Effect |
|---|---|---|---|---|
| Strike | Basic | Attack | 1 | Deal 6 damage. (×4) |
| Defend | Basic | Skill | 1 | Gain 5 Block. (×4) |
| Etch | Basic | Attack | 1 | Inscribe [Strike 7]. |
| Echo Sign | Basic | Skill | 1 | Inscribe [Echo]. (Upg: cost 0) |
| Blood Etching | Common | Attack | 1 | Inscribe [Strike 14 + Blood 3]. |
| Scatter Marks | Common | Attack | 1 | Inscribe [Seek][Strike 4][Strike 4]. |
| Ward Rune | Common | Skill | 1 | Inscribe [Ward 7]. |
| Nova Sigil | Common | Skill | 0 | Inscribe [Nova]. |
| Quick Carve | Common | Attack | 0 | Deal 3 damage. Inscribe [Strike 3]. |
| Hexing Mark | Common | Skill | 1 | Inscribe [Hex 1 + Expose 1]. |
| Open Circle | Uncommon | Skill | 1 | Inscribe [Loop 2]. |
| Close Circle | Uncommon | Skill | 0 | Inscribe [End Loop]. Draw 1. |
| Amplify Rune | Uncommon | Skill | 1 | Inscribe [Amplify +4]. |
| Invoke | Uncommon | Skill | 1 | Speak your Incantation now. Exhaust. |
| Kindling Mark | Uncommon | Skill | 0 | Inscribe [Kindle 1 + Blood 2]. |
| Mending Glyph | Uncommon | Skill | 1 | Inscribe [Mend 3]. Exhaust. |
| Resonance | Uncommon | Power | 1 | Gain 1 Resonance. |
| Grand Circle | Rare | Skill | 2 | Inscribe [Loop 3]. |
| Twin Sigil | Rare | Skill | 1 | Inscribe [Twin ×2]. |
| Seal of Patience | Rare | Skill | 1 | Inscribe [Seal]. Retain. |
| Blood Covenant | Rare | Attack | 2 | Inscribe [Strike 30 + Blood 8]. |
| Scriptorium | Rare | Power | 2 | At the start of your turn, Inscribe [Strike 4]. |

Starter relic options: **Chalk Stylus** — "At the end of your turn, if your Incantation is empty, Inscribe [Ward 4]." or
**Graven Stone** — "The first glyph you Inscribe each combat is Echoed."

## 10. Roadmap
1. **Phase 0 – Scaffolding**: replace Ironclad starters/relic; add minimum card set with placeholder art; keyword + hover tips.
2. **Phase 1 – Rune core**: `Rune`/`Glyph`/`RuneBuffer`/`RuneProgram`/`RuneInterpreter`/`RuneCmd`, `IncantationPower` (hover-tip UI), payloads Strike/Ward/Blood/Mend, targets Anchor/Seek/Nova, modifiers Amplify/Twin, Loop/End. Logging of each step.
3. **Phase 2 – Cards**: the §9 list, starter relic, localization.
4. **Phase 3 – Overhead UI**: `NRuneBuffer` above head, step highlighting during Speak, fizzle/overload VFX, loop brackets.
5. **Phase 4 – Expansion**: Chain/Cull/Mirror, Echo/Seal, Kindle/Insight/Hex/Expose, Resonance, curse runes, Invoke timing, capacity slots (orb-like), damage preview on hover, ~75 cards, relics, potions, balance pass.

## 11. Open questions
- Player-facing names: Incantation / Glyph / Speak / Fizzle — keep?
- Strike runes as powered attacks (proposed; Strength applies per hit) vs Unpowered with rune-only debuffs?
- Default target: Anchor (proposed) vs Random.
- Should `Mend` exist in v1 given heal strength?
- Clear every turn (proposed) vs persistent buffer with Seal as the only retention?
