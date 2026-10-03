# Rune System Design — "The Incantation"

Status: **v0.10 — Ancients & Ascended cards (10/3/2026)** (rune core, 98 cards incl. 6 special, 13 relics, 4 potions, card-slot Imbue, overhead UI, capacity, forecast, co-op rune sharing, Growth/Reflection/Friendship/Diminish/Clone runes, Persist, Fizzle visuals).
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
| **Rune** | Atomic instruction: `Strike 5`, `Loop`, `Scatter`. | `Rune` |
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
are always their own glyph (a card can inscribe several glyphs, e.g. `[Loop][Strike 4][End]`). This keeps the
grammar trivially parseable and the visuals readable.

**Merging:** when a cast's *first* glyph matches the glyph currently at the end of the Incantation (same kind, same
runes in the same order, same anchor), they merge and values add: `[Strike 6]` + Strike Rune → `[Strike 12]`,
`[Strike 14 + Blood 3]` × 2 → `[Strike 28 + Blood 6]`, Echo + Echo → Echo +2.
Mergeable runes implement `Rune.WithValue`; targets, Loops, End Loop, Seal and Sanctify never merge (so `[Loop]` +
`[Loop]` nests instead of merging; Loops have no value, so nothing can change their count).
A single card's own glyph sequence (Double Stroke's two Strikes) stays separate, so multi-hit cards keep their
per-hit behaviour. Merging does not use a capacity slot. A glyph delayed by a Growth/Overgrowth (the glyph right after it)
never merges: otherwise `[Growth 1][Strike 5]` + Strike Rune would grow every later cast too. Persistent glyphs (Patient
Rune, Persist…) still merge as usual.

## 4. Base rune set (v1)

### Payloads
| Rune | Effect | Amplifiable¹ | Notes |
|---|---|---|---|
| **Strike X** | Deal X damage to the current target(s). | ✔ | Powered attack (`ValueProp.Move`) so Vulnerable/Weak/Strength apply per hit, like multi-hit attacks (see §7). |
| **Defend X** | Gain X Block. | ✔ | Powered (`ValueProp.Move`) so Dexterity/Frail apply. Block at end of turn still protects during enemy turn. |
| **Mend X** | Heal X HP. | ✔ | Strong in StS — keep rare, low values, often Exhaust. |
| **Hex X** | Apply X Weak **and** X Vulnerable to target(s). | ✔ | Order matters: Hex before Strike amplifies later hits. |
| **Cleanse** | Remove all debuffs from you and every [Blood] rune from the Incantation. | ✘ | Self-targeted (ignores target mode). Valueless (internally 1); never merges. Blood is stripped from every glyph (a Blood-only glyph is removed, not fizzled) for the rest of the Speak and from whatever is kept afterwards, even under `keep` (Ritual, Chant, Eternal Sigil, Lingering Aroma). The forecast shows it too. |
| **Kindle X** | Gain X Energy. | ✘ | End-of-turn → `EnergyNextTurnPower`. Invoked → immediate. |
| **Swift X** | Draw X cards. | ✘ | End-of-turn → `DrawCardsNextTurnPower`. Invoked → immediate. |
| **Blood X** | Lose X HP (unblockable, self). | ✔ | The drawback half of compound glyphs (Ritual, Blood Sacrifice, Cursed Sword). |
| **Diminish X** | Deal X damage (as Strike), then Persist with its value halved; once the halved value drops below 5 the rune fizzles away instead of persisting. | ✔ | Halves **in place after every execution** (Echo, later loop passes and a Reflection hit for less); the halved value carries into next turn. |

¹ `Rune.Amplifiable`: only amplifiable runes are changed by Amplify/Twin/Sanctify (`ModifierRune.ApplyTo`), Amplify
Sigil and Thrumming Elixir. Non-amplifiable ones (Cleanse, Kindle, Swift, and every non-payload rune such as Loop, Echo,
Amplify) keep their value, but still repeat in loops.

### Imbue (instant, not a rune)
Every card has one **Imbue slot** (`RunePriestCard.ImbuedRunes`, encoded by `GlyphCodec`). It is only set on the
combat copy of the card (not written to the deck card), so Imbues last until the end of combat — unless the **Aether Quill**
potion copies it onto the deck card (`RunePriestCard.MakeImbuePermanent`; `ImbuedRunes` is a `[SavedProperty]`, so a
permanent Imbue is saved with the run and every later combat copy starts Imbued). **Imbue X** on a card with a free slot removes the X most recently
inscribed glyphs (any kind, newest first; `RuneCmd.TakeForImbue`) and binds them to the card. Once filled, the slot
can't be overwritten: playing the card Inscribes the bound glyphs instead (anchored to the card's target), before the
card's other effects. While an Imbued `Self`/`None`-target card holds an enemy-targeting payload (Strike, Hex…), its
`TargetType` becomes `AnyEnemy`, so the player picks the enemy the bound runes are anchored to instead of a random one.
A card may pass a filter to choose which runes it takes. Listeners get `AfterImbued` per glyph
(Paladin Sigil). Once filled, the card's own text shows the bound runes (`ImbuedCount` / `ImbuedRunes` description
vars added in `RunePriestCard.AddExtraArgsToDescription`; loc uses `{ImbuedCount:choose(0):<imbue text>|{ImbuedRunes}}`).
Cards: Blank Rune, Spellbook, Transmute, Ancient Tablet (every rune), Holy Smite (bonus per bound
rune), Imbued Sword (bonus = total bound value), Imbued Shield (doubled Block while Imbued; playing it releases the
runes and empties the slot), Alchemize+, Folly's Mirror+, Frenzied Incant+. Conjure Portal and Nyx react to Imbues.

### Modifiers (apply to the **next glyph**)
| Rune | Effect |
|---|---|
| **Amplify +X** | +X to every amplifiable payload in the next glyph. |
| **Twin ×N** | Multiply every amplifiable payload in the next glyph by N (cards inscribe ×2; two Twins merge to ×4). |
| **Echo** | Execute the next glyph one extra time. |
| **Sanctify** | Blood runes in the next glyph (or whole loop body) resolve as 0. Engine-only. |
| **Void** | **Consumes** the next glyph — any kind (another Void, a Loop, a Growth…); targets and End Loops are transparent. The consumed glyph fizzles once and is gone for the rest of the Speak (and isn't kept). The Void itself stays, so every later loop pass or Reflection consumes again. A consumed Loop's End Loop stays and fizzles. Dark Magick, Darkness Falls, Nebula, Dark Star, Waning Moon. |
| **Growth X** | Delays the next glyph: **every trigger** (so every loop pass) ticks the Growth down and **doubles that glyph in place**, keeping it for next turn instead of resolving it (`[Loop][Growth 3][Strike 3]` → Growth 1 + Strike 12 next turn). Ticked down to 0, the Growth vanishes and the glyph resolves on its next trigger (so `[Growth 1][Strike]` waits exactly one turn). Reached while its Growth still lives (after a Reflection) it just waits. Handled by the interpreter (not `ApplyTo`). New casts never merge into the glyph it is growing (§3). |
| **Overgrowth X** *(Ascended)* | Growth that **triples** the delayed glyph each trigger (`GrowingRune.Factor` 3). Only the Overgrowth Ancient card inscribes it. |
| **Reflection** | The Speak turns around: earlier glyphs are Spoken again in reverse order **as earlier runes left them** (consumed glyphs gone, grown/diminished values kept); loops still open stop repeating; glyphs after the Reflection are never Spoken. Handled by the interpreter. |
| **Friendship** | Co-op: the next supportive (ally) payload — value > 0: every later one — affects **all living players** instead of only the caster. Handled by the interpreter. |
| **Clone** | Persists; each trigger duplicates the following glyph into next turn's Incantation as a Persistent copy. Nothing after it → fizzles (unless the Clone itself Persists). Handled by the interpreter. |

Rules:
- Pending modifiers **stack and apply in inscription order**: `Amplify 3, Twin ×2, Strike 5` → (5+3)×2 = 16; `Twin ×2, Amplify 3, Strike 5` → 5×2+3 = 13. Ordering *is* the skill expression.
- A modifier applies to **all** amplifiable payloads in the glyph — including `Blood`. Doubling `Strike 14 + Blood 3` doubles both.
- **Target glyphs are transparent**: modifiers pass through them to the following glyph.
- If the next glyph is a **Loop**, the modifier applies to **the entire loop body on every iteration**.
- A modifier with nothing valid after it (end of Incantation, `Seal`) **fizzles**.
- Loop boundaries are transparent: a modifier at the end of a loop body **rolls over** to the first glyph of the next iteration (`[Loop][Strike 6][Echo]` → the second pass strikes twice), and after the last iteration to the glyph after the loop. Target modes already persist across iterations.

### Targets (persist until changed)
Each payload rune declares a `RuneTargeting`: **Enemy** (Strike, Hex), **Ally** (Defend, Mend), or **Self**
(Cleanse, Blood, Kindle, Swift — always the caster, ignores target mode). The mode picks the enemies:

| Rune | Enemy payloads | Ally payloads |
|---|---|---|
| *(default)* **Anchor** | The enemy targeted when the glyph's card was played; if dead/none → random enemy. | You. |
| **Scatter** | Random enemy, re-rolled **per execution** (loops scatter). | You. |
| **Nova** | All enemies. | You. |
| **Execution** | Lowest current HP enemy. | You. |
| **Mirror** *(curse rune)* | **You.** | A random enemy. |

**Co-op rule: runes only affect the player who inscribed them** (and the enemies). Ally payloads never reach other
players, whatever the target mode, so every player's Incantation behaves the same whether or not teammates have
runes of their own. The only way to give runes to other players is **Choral Evocation** (§14).

Mirror is a mode like the others: it lasts until the next targeting rune, so any target-control card counters it.
`[Mirror][Strike][Block][Strike]` → both Strikes hit you and the Block goes to an enemy.

Mirror is only for curses/status/enemy effects — no player card inscribes it yet.

### Flow
| Rune | Effect |
|---|---|
| **Loop** | Speak the glyphs up to the matching `End Loop` twice. Valueless (like targets): Loops never merge and nothing changes their count; nest Loops for more passes (`[Loop][Loop]…[End][End]` = 4). Echo before a Loop multiplies its passes. |
| **End Loop** | Closes the innermost open Loop. |
| **Seal** | Stop speaking. Glyphs **after** the Seal stay in the Incantation for next turn (Seal is consumed). Enables multi-turn setups. |

### Persist (keyword)
Persistent glyphs (`Glyph.Persistent`, set per glyph with `.Persist()`) stay in the Incantation after being
Spoken. Card text writes `[gold]Persist[/gold].` inline right after the rune it applies to (`Inscribe [Void]. Persist.`);
those cards don't list the `RunePriestKeywords.Persist` keyword (it would print a second "Persist") and add its hover
tip via `AdditionalHoverTips` instead. Persistent glyphs they stay in the Incantation after being
Spoken instead of being cleared (Patient Rune, Cocoon, Dark Star, Curse, Cloning Rune), and are drawn with a white outline square in the
overhead UI. A Persist *modifier* with nothing after it doesn't
fizzle — it just stays (Dark Star's Void). Growth and Diminish glyphs manage their own carry-over, so a Persist Growth
still vanishes once fully grown and a Diminish still fizzles once halved below 5. Merging keeps Persist if either half
had it.

## 5. Evaluation semantics
The interpreter Speaks a **tape of slots**, one per inscribed glyph. Self-modifying runes rewrite slots as they run
(Void marks the next slot consumed, Growth doubles the next slot and ticks itself down, Diminish halves itself), so
later loop passes and a Reflection (which reverses the same slots) see the changed Incantation. What is kept for next
turn is read off the slots at the end (spoken Persist glyphs, Growth/Diminish carry-overs, Clone copies, post-Seal glyphs).
```
ctx = { target: Anchor, pendingMods: [], loopStack: [], budget: MaxPayloadExecutions (default 60), timing: EndOfTurn|Invoked }
pc = 0
while pc < glyphs.Count:
    g = glyphs[pc]
    switch g.kind:
      Target   -> ctx.target = g.mode                                  ; pc++
      Modifier -> ctx.pendingMods.Add(g)                               ; pc++
      LoopOpen -> push {start: pc+1, remaining: 2 × (1+echoes), mods: take(pendingMods)} ; pc++
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
- Listener adjustments (e.g. Amplify Sigil) apply to the base value **before** modifiers, so Twin doubles them too. Thrumming Elixir rewrites the base value itself.
- Targets are resolved **at execution time**, so a loop keeps working as enemies die. Dead/untargetable anchor → random.
- If all enemies die, combat ends and evaluation stops (hooks stop firing anyway). Remaining glyphs just vanish.
- If Blood kills the player, the player dies. That's the risk; relics/cards can mitigate.
- `Overload` (budget exhausted): remaining glyphs crack and fizzle. v1 = harmless. Optional balance lever: backlash damage.

### Fizzle table (never throws)
| Situation | Result |
|---|---|
| Modifier with no following payload/loop | Fizzles |
| Target glyph overridden by another target before any payload used it | Fizzles (Nova + Nova = the first one fizzles) |
| Target glyph with no later payload | Fizzles at the end of the Speak (Persist targets just stay) |
| `End Loop` with no open loop | Fizzles |
| `Loop` never closed | Implicitly closes at end |
| Diminish halved below 5 | Fizzles instead of persisting |
| Glyph consumed by a Void | Fizzles once; skipped for the rest of the Speak |
| End Loop of a consumed Loop | Fizzles |
| Clone with nothing after it | Fizzles (a Persistent Clone just stays) |
| Payload with no valid target | That payload fizzles; others in the glyph still resolve |
| Budget exceeded | Overload: rest fizzles |
| Unknown/corrupt rune (e.g. from removed content) | Fizzles |

## 6. Example programs
- `[Strike 5]` → 5 damage to anchor.
- `[Loop][Strike 4][End]` → 8 damage.
- `[Twin ×2][Loop][Strike 14 + Blood 3][End]` → Twin empowers the whole loop: 56 damage, **12 HP loss**. Adding `[Mend 3]` inside the loop gets doubled too (heal 6/iteration) and fully cancels the Blood — placement is the puzzle.
- `[Scatter][Loop][Loop][Strike 3][End][End]` → 4 random 3-damage hits.
- `[Loop][Strike 3 + Defend 3][End]` (Quick Scribe+) → 6 damage and 6 Block.
- `[Loop][Strike 3][Defend 3][End]` → same result; Runic Form wraps the first two runes of every turn like this.
- `[Hex 2][Nova][Strike 6]` → Hex hits the anchor only (Nova comes after), then 9 to the Vulnerable anchor and 6 to the rest.
- `[Strike 8][Seal][Loop]` → 8 now; `[Loop]` waits at the front of next turn's Incantation.

## 7. Balance principles
- **Delay is almost free** in StS (enemies act after end of turn), so base rune values should sit **below** plain Strike/Defend (Strike rune ~5 vs Strike 6). The upside is composability.
- Rune damage/block is **powered**, because `VulnerablePower`/`WeakPower` only affect powered attacks (`props.IsPoweredAttack()`), and Unpowered runes would make Hex pointless. Consequence: Strength/Dexterity apply per payload execution, exactly like base-game multi-hit attacks — so loops are "multi-hit" and must be costed that way. Flat scaling comes from **Amplify Sigil** (+N to every amplifiable payload) and Thrumming Elixir. (The Strength rune was removed in the 9/30 sync for exactly this loop-scaling reason.) Fallback lever if Strength loops break balance: make Strike runes Unpowered and have Hex apply bespoke rune-only debuffs.
- Non-amplifiable Kindle/Swift prevent multiplier abuse; loops still repeat them → keep them rare and consider a per-Incantation cap (lever).
- Loops and Twin are the explosive pieces → Uncommon/Rare, higher cost, or Exhaust.
- Drawbacks (Blood, Mirror) scale with the same modifiers as the upside — self-balancing by design.
- Levers available without redesign: `MaxPayloadExecutions`, Overload backlash, Incantation capacity (future orb-like slots), per-rune caps.

## 8. Architecture (implemented)
```
RunePriestCode/
  Runes/
    Rune.cs               abstract base: Value, Kind, Key (loc key RUNEPRIEST-RUNE_<Key>), Label, HoverTips
    PayloadRunes.cs       PayloadRune (Amplifiable, Targeting, Resolve) + Strike/Defend/Mend/Cleanse/Blood/Kindle/Swift/Hex/Diminish
    ModifierRunes.cs      ModifierRune (ApplyTo, ExtraExecutions, Voids) + Amplify/Twin/Echo/Sanctify/Void/Growth/Reflection/Friendship/Clone
    TargetRune.cs         TargetRune(TargetMode)
    FlowRunes.cs          Loop/EndLoop/Seal
    Glyph.cs              runes + Anchor + Source card; Empower/Scaled/DefendAsMend transforms; Kind == null means malformed
    GlyphCodec.cs         glyph list <-> text ("KEY:VALUE,...;...") for Imbued cards
    RuneBuffer.cs         ordered glyphs, Inscribe/TakeAll/Retain, `event Changed`
    RuneProgram.cs        pure loop-bracket matching
    RuneInterpreter.cs    executes glyphs (loops, modifiers, echo, seal, budget, fizzles, logging)
    RuneContext.cs        per-Speak state + target resolution
    RuneCmd.cs            Inscribe / Share / ShareAt / Prepend / Speak / SpeakAt / TakeForImbue / Fizzle / Remove / Transform / Forecast / GetBuffer
    RuneTips.cs           static hover tips (Inscribe, Speak, Imbue, Imbued, Overflow, Incantation script title)
    IRuneListener.cs      ModifyRuneValue / ReplacePayload / ModifyCapacity / KeepsIncantation / ModifyInscription / AfterInscribed / AfterImbued / AfterRemoved / AfterPayload / AfterFizzle / AfterSpeak (+ RuneListeners helper)
    RunePreview.cs        dry-run totals, shown values and per-glyph targets; PreviewEffects = powers earlier runes will have applied
  Powers/
    IncantationPower.cs   hosts RuneBuffer (InitInternalData → fresh per clone); Speaks in BeforeSideTurnEnd; DisplayAmount = glyph count; hover tip lists glyphs
    AmplifySigilPower.cs  IRuneListener: every rune Spoken as if preceded by Amplify +Amount (amplifiable runes only)
    RunicFormPower.cs     ModifyInscription: wraps the first two glyphs each turn in [Loop] … [End Loop]
    OddSigilPower.cs      ModifyInscription: every 2nd glyph each turn is Scaled(2)
    ChoralEvocationPower.cs  AfterInscribed: shares your Defend runes (Plus: every rune) with the other players this turn
    TurnCounter.cs        mutable per-turn counter for power InitInternalData
  Cards/
    RuneCard.cs           abstract Glyphs(Creature? anchor); OnPlay → RuneCmd.Inscribe; auto hover tips for Inscribe + runes; Var(name)
    Basic/ Common/ Uncommon/ Rare/
  Relics/BlessedToolbox.cs starter (random Common rune card on pickup; first rune card each combat → draw 1)
  Nodes/
    NRuneBuffer.cs        row above a player's head; polls RuneCmd.GetBuffer, diffs glyphs by reference, lays out (ReadRightToLeft const)
    NGlyph.cs             one slot; compound runes stacked vertically; appear / pulse (activated) / shake+grey (fizzle) / dissolve
    NRuneSymbol.cs        script character + value label, random bobbing, additive CpuParticles2D scaled by value
    NVoidVortex.cs        Void rune: dark hollow ring + screen-edge motes spiralling in (black hole)
    RuneVisuals.cs        style table: family → colour, effect → script, value → character
    RuneFont.cs           composite FontVariation from the game's bundled jpn/kor/tha/rus fonts; HasChar fallback
  Patches/NCreatureRuneBufferPatch.cs  Harmony postfix on NCreature._Ready → attach NRuneBuffer for players
  Patches/NeowAetherQuillPatch.cs      Harmony postfix on Neow.GenerateInitialOptions → sometimes offers the Aether Inkwell
  Patches/AncientOptionPatches.cs      postfixes on Darv/Vakuu/Tezcatara → Dark Tablet / Corrupted Sigil / Eternal Candle (§15)
```
Key decisions:
- **Buffer lives on a Power** (`IncantationPower`) on the player creature: auto-receives hooks with a correctly owned `PlayerChoiceContext` (co-op safe), auto-cleans at combat end, and its icon + hover tip is the v1 UI. Applied lazily on first Inscribe; stays for the rest of combat (Amount fixed at 1).
- Evaluate in `BeforeSideTurnEnd`. Relics that inscribe at end of turn must use `BeforeSideTurnEndEarly`.
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
| Defend Rune (`DefendRuneCard`) | Basic | Skill | 1 | Inscribe [Defend 4]. (×5) | cost 0 |
| Echo Rune (`EchoRuneCard`) | Common | Skill | 1 | Inscribe [Echo]. | cost 0 |
| Nova Rune | Common | Skill | 1 | Inscribe [Nova]. | cost 0 |
| Chain Lightning | Common | Attack | 2 | Inscribe [Scatter][Strike 3]×3. | cost 1 |
| Execution Rune | Common | Attack | 1 | Inscribe [Execution][Strike 10]. | Strike 15 |
| Hex Rune | Common | Skill | 1 | Inscribe [Hex 1]. | cost 0 |
| Blind Rage | Common | Attack | 1 | Inscribe [Scatter][Strike 15]. | Strike 20 |
| Quick Jab | Common | Attack | 0 | Deal 3. Inscribe [Strike 3]. | 4 / 4 |
| Mending Rune | Common | Skill | 1 | Inscribe [Mend 3]. Exhaust. | Mend 5 |
| Runic Barrage | Common | Attack | 1 | Deal 3 damage per rune in the Incantation (one hit each). | 4 |
| Quick Scribe | Common | Attack | 1 | Inscribe [Strike 3 + Defend 3]. | wrapped in [Loop]…[End Loop] |
| Amplification Rune | Common | Skill | 1 | Inscribe [Amplify +4]. | +6 |
| Blank Rune | Common | Skill | 0 | Imbue 1. | also draw 1 |
| Hasty Scrawl | Common | Attack | 0 | Inscribe [Strike 3][Swift 1]. | Swift 2 |
| Meditate | Common | Skill | 2 | Inscribe [Mend 4][Defend 4]. Exhaust. | cost 1 |
| Amplified Strike | Common | Attack | 1 | Inscribe [Strike 5][Amplify +1]. | 6 / +2 |
| Flint & Steel (`FlintAndSteel`) | Common | Skill | 1 | Inscribe [Blood 5][Kindle 1]. | cost 0 |
| Quick Ward | Common | Skill | 0 | Inscribe [Defend 3 + Swift 1]. | also draw 1 on play |
| Heavy Ward | Common | Skill | 2 | Inscribe [Defend 12]. | Defend 15 |
| Heavy Rune | Common | Attack | 2 | Inscribe [Strike 9 + 3 per rune in the Incantation]. | +4 per rune |
| Patient Rune (`PatientRuneCard`) | Uncommon | Attack | 1 | Inscribe [Strike 1 + 1 per rune inscribed this combat]. Persist. Exhaust. | Strike 5 |
| Cracked Rune (`CrackedRuneCard`) | Common | Attack | 1 | Inscribe [Strike 2]×2 (separate, so they don't merge). | ×3 |
| Growth Rune (`GrowthRuneCard`) | Common | Skill | 1 | Inscribe [Growth 3]. | cost 0 |
| Imbued Sword | Common | Attack | 1 | Imbue 1. Deal 5 + total value of its bound runes. | Imbue 2 |
| Inquire | Common | Skill | 0 | Draw 1. Inscribe [Swift 1]. Exhaust. | Swift 2 |
| Star Sigil | Uncommon | Power | 1 | Start of turn: Inscribe [Nova]. | also draw 1 on play |
| Nova Slice | Uncommon | Attack | 1 | Inscribe [Nova][Strike 6][Nova]. | Strike 9 |
| Echoing Ward | Uncommon | Skill | 1 | Inscribe [Defend 8][Echo]. | Defend 11 |
| Loop Rune | Uncommon | Skill | 1 | Inscribe [Loop]. | draw 1 |
| Twin Rune | Uncommon | Skill | 1 | Inscribe [Twin ×2]. | draw 1 |
| Kindle Rune | Uncommon | Skill | 1 | Inscribe [Kindle 1]. | cost 0 |
| Warding Sigil | Uncommon | Power | 1 | After Speak: 2 Block per rune Spoken. | 3 |
| Swift Rune | Uncommon | Skill | 1 | Inscribe [Swift 2]. | cost 0 |
| Amplify Sigil | Uncommon | Power | 2 | Every rune is Spoken as if preceded by [Amplify +1]. | cost 1 |
| Swift Sigil | Uncommon | Power | 1 | Rune removed without being Spoken (Alchemize, fizzle) → draw 1. | cost 0 |
| Alloy (`Alloy`, was Alchemize) | Uncommon | Skill | 0 | Remove the last rune; if one was removed, draw 2. | Imbue 1 first (an Imbued Alloy Inscribes its bound runes before removing, so it always draws) |
| Holy Water | Uncommon | Skill | 1 | Inscribe [Cleanse][Mend 4]. Exhaust. | cost 0 |
| Twin Blades | Uncommon | Attack | 2 | Inscribe [Twin ×2][Strike 10]. | Strike 12 |
| Candlelight | Uncommon | Skill | 2 | Inscribe [Kindle N], N = runes in the Incantation (simultaneous runes each count). | cost 1 |
| Spellbook | Uncommon | Skill | 1 | Imbue 3. | cost 0 |
| Transmute | Uncommon | Skill | 1 | Imbue 1. Inscribe [Swift 3]. | Imbue 2 |
| Holy Smite | Uncommon | Attack | 1 | Imbue 2. Deal 11 + 5 per rune Imbued into it. | 14 / 6 |
| Pacify | Uncommon | Skill | 1 | Every [Defend] rune becomes [Mend] of the same value. Exhaust. | cost 0 |
| Holy Water Sigil | Uncommon | Power | 1 | Healing deals that much damage to a random enemy. | ALL enemies |
| Barrier of Light | Uncommon | Skill | 2 | Until your next turn, damage taken is halved. | cost 1 |
| Flow State | Uncommon | Skill | 1 | This turn, whenever you Inscribe, draw 1. Exhaust. | no Exhaust |
| Paladin Sigil | Uncommon | Power | 2 | Whenever you Imbue a rune, gain 1 Energy. | cost 1 |
| Conjure Portal | Uncommon | Power | 1 | Multiplayer only. Whenever you Imbue a rune, Inscribe it for a random other player (`RuneCmd.Share`). | cost 0 |
| Chaos Falls | Uncommon | Power | 2 | Multiplayer only. Start of turn: Inscribe [Scatter][Twin ×2][Loop]. Scatter picks from everyone, players included (`IRuneListener.ScatterTargetsAnyone`). | cost 1 |
| Star Shield | Uncommon | Skill | 1 | Gain 4 Block per [Nova] in the Incantation. | 5 |
| Reflection Rune (`ReflectionRuneCard`) | Uncommon | Skill | 1 | Inscribe [Blood 2][Reflection]. | cost 0 |
| Blood Magick | Uncommon | Attack | 1 | Inscribe [Hex 1][Strike 6][Blood 2]. | Hex 2 |
| Leeches | Uncommon | Skill | 0 | Inscribe [Blood 2][Cleanse]. Exhaust. | no Exhaust |
| Cocoon | Uncommon | Skill | 1 | Inscribe [Growth 2][Defend 2 (Persist)]. | Defend 3 |
| Nebula | Uncommon | Attack | 2 | Inscribe [Nova][Twin ×2][Loop][Strike 5][End Loop][Void] (the Void fizzles harmlessly). | no End Loop: the Void — and anything inscribed after — joins the loop |
| Nyx | Uncommon | Power | 0 | Whenever you play a card Imbued with a [Void], deal 20 (Unpowered) to ALL enemies. | 30 |
| Eternal Sigil | Rare | Power | 3 | Incantation is kept after Speaking. Ethereal. | no Ethereal |
| Runic Form | Rare | Power | 3 | First two runes inscribed each turn are wrapped in [Loop]…[End Loop]. | cost 2 |
| Engorged Strike | Rare | Attack | 1 | Inscribe [Strike 8][Amplify +2]. | +3 |
| Blessing | Rare | Skill | 1 | Inscribe [Loop][Twin ×2]. Exhaust. | no Exhaust |
| Odd Sigil | Rare | Power | 2 | Every 2nd rune inscribed each turn has its values doubled. | cost 1 |
| Trick of the Light | Rare | Skill | 0 | Inscribe [Swift 3]. Exhaust. | Swift 4 |
| Ancient Tablet | Rare | Skill | 2 | Imbue every rune. | cost 1 |
| Friendship Rune (`FriendshipRuneCard`) | Rare | Skill | 1 | Multiplayer only. Inscribe [Friendship]: the next supportive rune affects every player. | all later supportive runes |
| Orobas Strike | Rare | Attack | 2 | Inscribe [Loop][Growth 6][Strike 3][End Loop]; the Loop and End Loop Persist. | cost 1 |
| Black Hole Strike | Rare | Attack | 1 | Inscribe [Loop][Strike 10]. Add a White Hole Strike to the discard pile. Exhaust. | to the draw pile |
| Stacked Strike | Rare | Attack | 2 | Inscribe [Strike 7]×3 (separate, so they don't merge). | cost 1 |
| Imbued Shield | Rare | Skill | 1 | Gain 9 Block, doubled while Imbued. Imbue 1; playing it Imbued releases its runes and empties the slot. | 11 |
| Dark Star | Rare | Power | 0 | Inscribe [Void]. Persist. While a [Void] is inscribed, draw 1 at the start of each turn. | draw 2 |
| Ritual | Common | Skill | 0 | Inscribe [Blood 5]. Speak; the runes remain (`keep: true`). | Blood 3 |
| Spark Strike | Common | Attack | 1 | Fizzle the last rune; if one fizzled, deal 8. | 11 |
| Chant | Uncommon | Skill | 2 | Speak; the runes remain (`keep: true`). | cost 1 |
| Blood Sacrifice | Uncommon | Attack | X | Inscribe [Blood 1] X times, [Strike X] X times. | also [Echo] after the Blood runes; [Strike X+1] X+1 times |
| Darkness Falls | Uncommon | Attack | 0 | Inscribe [Void][Strike 30]. | Strike 40 |
| Flagellation | Uncommon | Power | 2 | Whenever you lose HP during your turn, Inscribe [Defend 4]. | cost 1 |
| Frenzied Incant | Uncommon | Skill | 0 | Inscribe [Loop]. Put [Scatter] at the start of the Incantation. | Imbue 1 first |
| Folly's Mirror | Uncommon | Skill | 0 | Inscribe [Echo][Nova][Echo][Nova]. | Imbue 1 first |
| Energy Overflow | Uncommon | Power | 1 | Whenever a rune fizzles, deal 5 damage to ALL enemies. | cost 0 |
| Dark Magick | Rare | Skill | 1 | Inscribe [Void]. Add 2 upgraded Loop Runes to your hand. | Inscribe [Kindle 1] first |
| Unforgiveable Curse | Rare | Power | 1 | Whenever a rune fizzles, add a random cursed item to your hand (end-of-turn fizzles deliver next turn). | also Inscribe [Nova] ×3 (two of them fizzle unused → two cursed items) |
| Choral Evocation | Rare | Skill | 1 | Multiplayer only. This turn, [Defend] runes you Inscribe are also Inscribed for all other players. Exhaust. | every rune is shared |
| Waning Moon | Uncommon | Attack | 2 | Inscribe [Loop][Strike 4][Defend 4][Void (Persist)][Defend 4][Strike 4][Reflection]. | Inscribe [Amplify +2] first |
| Diminishing Rune (`DiminishingRuneCard`) | Common | Attack | 2 | Inscribe [Diminish 20]. Exhaust. | cost 1 |
| Persistance | Common | Skill | 0 | Inscribe [Growth 1]. | also draw 1 on play |
| Cloning Rune (`CloningRuneCard`) | Rare | Skill | 2 | Inscribe [Clone (Persist)]. Add a copy of this card to the discard pile. | cost 1 |
| Clean Slate | Common | Skill | 0 | Remove the last rune. Inscribe [Void]. | also draw 1 on play |
| Unstable Ward | Common | Skill | 1 | Inscribe [Defend 8][Blood 3]. | Defend 10 |
| Reroute Energy | Uncommon | Skill | 2 | Remove ALL runes; gain 1 Energy per rune removed. | also add an Old Lantern to your hand |
| Last Scroll | Rare | Skill | 0 | Remove every rune after the first; wrap the first in 2 nested [Loop]…[End Loop] (4 passes). You may no longer Inscribe this turn (`LastScrollPower`). | 3 Loops (8 passes) |
| Two-way Mirror | Uncommon | Skill | 1 | Multiplayer only. Inscribe [Reflection]; Inscribe [Reflection] into a random slot of a random ally's Incantation (`RuneCmd.ShareAt`). | into the end of their Incantation instead of a random slot |
| Martyr | Uncommon | Skill | 1 | Inscribe [Blood 10][Mend 10][Reflection]. Exhaust. | no Exhaust |
| Exorcism | Uncommon | Power | 1 | The first Curse you draw each turn is Exhausted; then draw 1 and Inscribe [Void] (`ExorcismPower`, counted from the draw history like the base game's Iteration). | draw 2 |

Special (Token rarity, `TokenCardPool`; only created by Unforgiveable Curse / Cursed Spirits / Black Hole Strike / Reroute Energy+):

| Card | Type | Cost | Effect | Upgrade |
|---|---|---|---|---|
| Cursed Sword | Attack | 1 | Inscribe [Blood 10][Strike 20]. Deal 30. Ethereal. Exhaust. | Blood 5 |
| Cursed Armour | Skill | 1 | Can't gain Block until next turn; then Inscribe [Mend] = HP lost meanwhile. Ethereal. Exhaust. | also reflect HP damage from enemies until next turn |
| Cursed Spirits | Power | 1 | Summon 3 spirits (+1 each turn start); at end of turn each deals 5 (Unpowered, so Strength doesn't apply) to a random Black-Marked enemy; no marked enemy → no attack. Add a Black Mark to hand. Ethereal. Exhaust. | 5 spirits |
| Black Mark | Skill | 0 | Apply Black Mark (only marked enemies are attacked by spirits). | draw 1 |
| White Hole Strike | Attack | 1 | Inscribe [Strike 20][End Loop]. Add a Black Hole Strike to the discard pile. | cost 0 |
| Old Lantern | Skill | X | Inscribe [Kindle X]. Exhaust. | no Exhaust |

Totals: 2 Basic, 30 Common, 43 Uncommon, 18 Rare = 93, plus 6 special.

Assumptions made where the CSV was silent (revisit on review): bare "Inscribe Twin/Kindle/Hex" = Twin ×2 /
Kindle 1 / Hex 1; bare "Inscribe Swift" = Swift 2; "Draw 1" upgrades draw on play; Imbue takes the newest
runes first; Odd Sigil / Runic Form / Imbued Teacup count per turn; "removed" (Swift Sigil) = left the Incantation
without being Spoken, which includes fizzles; Holy Water Sigil uses the heal amount requested (even at full HP).

| Relic | Rarity | Effect |
|---|---|---|
| Blessed Toolbox | Starter | On pickup, add a random Common card with "Rune" in its name (matched on the model ID, so it is language-independent) to your deck. The first time you play a rune card each combat, draw 1. Replaced Chalk Stylus (removed in the 9/29 sync). |
| Holy Sparkler | Common | Whenever a rune fizzles, gain 4 Block. |
| Anchor Scroll | Common | At the start of combat, Inscribe [Defend 10]. |
| Execution Scroll | Common | At the start of combat, Inscribe [Execution][Strike 10]. |
| Imbued Teacup | Uncommon | The first rune inscribed each turn has its values doubled (`Glyph.Scaled(2)`; Loops are unaffected since #18). |
| Healer's Scroll | Uncommon | The first time each combat you Inscribe a [Blood] rune, Inscribe [Mend] equal to the Blood amount. |
| Baptised Idol | Uncommon | On pickup, upgrade 2 random cards that Inscribe (`RuneCard`s) — used instead of a "Rune" name match. |
| Runic Sphere | Uncommon | Cards with "Rune" in their name (`RunePriestCard.HasRuneInName`) have Retain (single-turn retain re-applied on draw and at turn start). |
| Enchanted Anvil | Rare | Whenever you Inscribe, deal 2 damage to ALL enemies. |
| De-illuminator | Rare | The first [Void] you Inscribe each combat immediately Fizzles (feeds fizzle triggers like Unforgiveable Curse). |
| Midas Hand | Rare | Whenever you Inscribe, gain 2 Gold. |
| Cursed Ring | Shop (200 Gold) | Cursed items (Cursed Sword, Cursed Armour, Cursed Spirits) cost 0 (`TryModifyEnergyCostInCombat`). |
| Undead Quill | Shop (300 Gold) | [Blood] and [Mend] runes swap their effects when Spoken (`IRuneListener.ReplacePayload`; side-effect-free so the forecast shows it too). |
| Lightweight Cloth Robe | Event | Cards with [Blood] runes (inscribed or Imbued; not X-cost) cost 0. Your [Blood] runes are doubled. From the Suspicious Tailor event. |
| Hallowed Toolbox | Starter | Blessed Toolbox upgraded by Orobas' Touch of Orobas (`CustomRelicModel.GetUpgradeReplacement`): whenever you play a rune card, draw 1. |
| Dark Tablet | Ancient (Darv) | Your [Blood] runes damage enemies and your [Mend] runes heal enemies instead of you (`ReplacePayload`; follow the target mode). |
| Corrupted Sigil | Ancient (Vakuu, pool 2) | Upon pickup, remove every Eternal Sigil from your deck and add a Corrupted Sigil card. Always offered to the Rune Priest. |
| Eternal Candle | Ancient (Tezcatara, pool 3) | Upon pickup, add an Eternal Scroll. Rest sites offer **Etch**: remove a rune card from your deck and etch its runes onto the Scroll. |
| Aether Inkwell | Ancient | Upon pickup, obtain an Aether Quill. Neow-only wrapper for the potion (see §14); Ancient rarity keeps it out of regular relic rewards. |

| Potion | Rarity | Effect |
|---|---|---|
| Echo Brew | Common | Inscribe [Echo]. |
| Teardrop | Common | Duplicate the last rune: a separate copy right after it (`RuneCmd.Duplicate`; never merges, no inscription listeners). Only usable with a non-empty Incantation. |
| Lucky Elixir | Uncommon | Inscribe [Swift 2, Mend 2] (one simultaneous glyph). |
| Snecko Decoction | Uncommon | Shuffle the values of every valued payload rune (Strike, Defend, Mend, Blood, Hex, Kindle, Swift, Diminish) in the Incantation (`CombatTargets` RNG). Modifiers, targets, flow and valueless runes are untouched. Only usable with 2+ such runes. |
| Lingering Aroma | Rare | This turn the Incantation is kept after Speaking (`LingeringAromaPower`, removed at your next turn start). |
| Thrumming Elixir | Rare | Amplify +2 every amplifiable rune already inscribed (`Glyph.Empower` rewrites the values in place). |
| Aether Quill | Event | Choose an Imbued card in your hand; its Imbue becomes permanent (only usable while such a card is in hand). Never a random potion; from Neow via Aether Inkwell. |

Balance watch-list: Blind Rage (15 for 1), Imbued Teacup + Runic Form (doubles the first rune inside the Loop),
Odd Sigil + Blessing, Eternal Sigil + loops (bounded by the 60-payload Overload), Choral Evocation+ with loops (every
player gets the whole program), permanent Imbues from Aether Quill, Growth doubling (compounds per trigger: Orobas Strike's persistent loop doubles its Strike twice per Speak, 3 → 12 → 48
→ 192, then hatches on turn 4 and hits twice;
Cocoon's Defend keeps persisting after it hatches), Nebula+ voiding everything inscribed after it, Dark Star +
Eternal Sigil (a persistent Void never fizzles away), Cloning Rune + Growth (free doubling every turn), Reroute
Energy emptying a huge Incantation, Undead Quill turning Martyr/Blood Sacrifice into pure healing.

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
- Design sync 9/29/2026 (issue #9): Strength rune grants **temporary** Strength; Cursed Spirits deal 5 (Unpowered) and only attack Black-Marked enemies; Imbued cards show their bound runes in their own text; Blessed Toolbox only offers Commons with "Rune" in the name; Ritual/Chant keep their runes after Speaking; new cards Flint & Steel, Darkness Falls, Flagellation. Assumptions: Blood Sacrifice+ puts its Echo after the Blood runes (before the Strikes); Flint & Steel's bare Kindle = Kindle 1; Flagellation triggers on unblocked damage taken while it is the player side's turn.
- Design sync 9/29/2026 (issue #12): Candlelight counts simultaneous runes individually (`IncantationRuneCount`); Imbue lasts only for the combat (no deck write-through / saved property); Imbued Self-target cards holding enemy runes target an enemy so the bound runes aren't random.
- Design sync 9/29/2026 (issue #14): runes only affect the player who inscribed them (ally payloads always hit the caster, Nova/Scatter/Execution/Anchor only pick enemies); new multiplayer-only Choral Evocation; new Event potion Aether Quill (permanent Imbue, via a saved `ImbuedRunes` written to the deck card) offered by Neow through the Aether Inkwell relic. See §14 for assumptions.
- Design sync 9/30/2026 (issue #16): **Strength rune removed** (with the Strength Rune card and `StrengthRunePower`) and **Holy Dagger removed**. New modifier runes **Growth**, **Reflection**, **Friendship** and the **Persist** keyword (`Glyph.Persistent`). Card fixes: Swift Sigil now procs on Imbue, Mending Rune 3/5, Frenzied Incant cost 0 with upgraded Imbue 1 first, Star Sigil+ only draws when played. 23 new cards (8 Common, 9 Uncommon, 6 Rare) plus the White Hole Strike token — see §9. Assumptions: Conjure Portal shares via `RuneCmd.Share` (no inscription listeners, so it can't chain with itself or Choral Evocation); Chaos Falls' "Scatter targets anyone" lasts while the power is active and includes the caster; Imbued Shield's Block doubling checks the Imbue state before the play resolves; Nebula+ literally drops the End Loop, so its trailing Void rolls into the loop; Black/White Hole Strikes add unupgraded copies of each other; Patient Rune has no upgrade (the CSV listed none).
- Design sync 10/1/2026 (issue #18): **Loops lost their mutable value** (`LoopRune` no longer implements `WithValue`): Loops never merge (adjacent Loop casts nest) and no value effect — Growth doubling, Amplify, Imbued Teacup — can change a loop count; Echo still repeats loop bodies and Twin/Amplify still distribute across them. **Growth ticks once per trigger** (a Growth inside a loop ticks every iteration). New runes **Diminish** (Strike-like payload that persists at half value and fizzles below 5) and **Clone** (persists; duplicates the following glyph into next turn as a Persistent copy). **Fizzle is now a keyword tip** with a visual (grey puff, lingering faded glyph), and unused target runes fizzle (overridden before use, or unused at the end of the Speak — so Unforgiveable Curse+ triggers off its own extra Novas). Debuff runes (Hex) get their own dark-purple family colour; Persistent glyphs are drawn with a white outline square. Card changes: Unforgiveable Curse+ inscribes 3 Novas (was Echoes), Paladin Sigil gives Energy (was Strength), Meditate 4/4 (was 5/5), Dark Magick+ Kindles first, Echo Rune moved to Common (starting deck is 5 Strike/5 Defend), Alchemize renamed **Alloy** (only draws if a rune was removed; Imbued Alloy+ inscribes first so it always draws), Folly's Mirror inscribes Echo+Nova twice, Choral Evocation costs 1, Orobas Strike's loop Persists. 11 new cards (4 Common, 4 Uncommon, 2 Rare + the Old Lantern token) and 5 new relics — see §9. Assumptions: Folly's Mirror "Echo + Nova" = sequential glyphs; Last Scroll wraps the first rune in a closed Loop and blocks further inscriptions via `LastScrollPower` (`ModifyInscription` → []); Two-way Mirror+ appends to the ally's Incantation; Diminish uses the inscribed value for every execution within one Speak and halves only the carried-over copy, fizzling when the halved result is below 5; "start of combat" relics inscribe on the owner's first turn start.
- Interpreter rework 10/1/2026: runes act on the Incantation **as it runs** (slot tape, §5). **Void consumes** the next glyph of any kind (so `[Void][Void][Strike]` strikes, and a Void before a Loop eats the Loop — its End Loop fizzles); the Void stays and consumes again on later loop passes and Reflections. **Growth doubles in place on every trigger** (compounds inside loops). **Diminish halves after every execution** (Echo included). A Reflection replays the slots as earlier runes left them (Waning Moon's Void eats a different Defend each way) and still stops open loops.
- Loops lost their number (10/1/2026): `LoopRune` is valueless like targets/Echo and always speaks its body twice; nest Loops for more (`IRuneListener.ModifyLoopCount` removed). Last Scroll now inscribes 2 nested Loops (3 upgraded) instead of Loop 5/8.
- Ancients pass 10/3/2026: Ascended cards moved to **Ancient rarity** (out of card rewards; never upgrade) and are reached through Orobas' Archaic Tooth; the stubbed ancient boons were implemented as real relics (§15). **Growth/Overgrowth can't be stacked on**: casts no longer merge into the glyph a Growth is delaying (regular Persist glyphs still merge). Mirrororrim now inscribes in `BeforeSideTurnEndEarly` (it used to race the Speak in `BeforeSideTurnEnd`, so its runes often landed after it and sat there next turn). Preview: earlier runes' effects (Hex) and teammates who Speak first now feed the overhead values; hovering a glyph highlights its targets with arcs. Patient Rune is Uncommon (so it is no longer a possible starting rune). Assumptions: Corrupted Sigil is always offered (Eternal Sigil removal is incidental); Etch removes the chosen rune card and etches its `RuneCard.InscribedGlyphs` (`GlyphCodec` now keeps Persist with a `!` prefix, which also applies to Imbued runes); Healer only adds Mend to payload glyphs.
- Card-pool pass 10/3/2026: Emanate renamed **Emenate**; Cocoon no longer inscribes a Void; Persist is written once, inline after its rune (keyword dropped from those cards); Unforgiveable Curse says "item"; Runic Sphere only affects cards with "Rune" in their name; Lightweight Cloth Robe is an **Event** relic (unobtainable until a Rune Priest event grants it) and its "Blood cards are free" half is now implemented.
- Curse pass 10/3/2026: the cursed items (Cursed Sword/Armour/Spirits) are **Ethereal + Exhaust**; **Cleanse** also removes every [Blood] rune from the Incantation; new Uncommon **Exorcism**, Shop relic **Cursed Ring** (200 Gold), potions **Teardrop**, **Lucky Elixir**, **Snecko Decoction**; Thrumming Elixir's description no longer mentions loops (they aren't amplifiable since #18). Assumptions: Cleanse stays self-targeted; Teardrop's copy is a separate glyph (no merge, no Inscribe triggers); Snecko Decoction only shuffles valued payload runes.

## 12. Overhead visuals (Phase 3)
From the user's sketch: runes float in a row over the head, **read left to right** (first glyph spoken is leftmost;
flip with `NRuneBuffer.ReadRightToLeft`). Compound glyphs stack vertically = "these resolve together".

- **Colour = family**: Offense red (Strike/Diminish), Debuff dark purple (Hex), Support green (Defend/Mend/Cleanse), Resource gold (Kindle/Swift),
  Cost crimson (Blood), Modifier violet, Target pink, Flow pale blue.
- **Shape = effect, character = value** (higher value → later/denser character):

| Rune | Script | Characters |
|---|---|---|
| Strike | Kanji by stroke count (step 3) | 刀刃斤矛伐戒刺剣殺斬裂戦撃闘轟 |
| Diminish | Kanji of decay (step 4) | 乙久斥朽衰減耗滅 |
| Defend | Greek (step 2) | αβγ…ωΩ |
| Mend | Hangul | 가나다…하 |
| Blood | Cyrillic | БГДЖ…Я |
| Kindle | Thai | กขค… |
| Swift | Katakana | アイウ… |
| Growth | Hiragana | あいう… |
| Hex | Bopomofo | ㄅㄆㄇ… |
| Cleanse | reference mark | ※ |
| Amplify / Twin / Echo / Sanctify / Reflection / Friendship | math | ⊕ ⊗ 〃 ⊘ ∽ ∪ |
| Clone | iteration mark | 々 |
| Anchor / Scatter / Nova / Execution / Mirror | symbols | ◎ ∴ ☆ ▽ ◇ |
| Loop / End Loop / Seal | CJK brackets / seal mark | 〔 〕 〆 |

- **Void** is the exception: no character, just a dark hollow ring (`NVoidVortex`) evoking a black hole. Sparse white motes
  spawn on the screen border, fade in, and spiral inward while accelerating until the ring swallows them. They start near
  transparent at the edge and brighten to normal particle opacity as they approach, so the full-screen effect stays subtle.
- Small numeric value label on valued runes (hover the Incantation power for full text).
- Motion: per-symbol random sine bob (cosmetic `GD.Randf`, not run RNG). Particles: additive `CpuParticles2D`, count/speed/size scale with value.
- Speak feedback: interpreter raises `RuneBuffer.GlyphActivated` / `GlyphFizzled` / `SpeakEnded`, with a short
  `Cmd.CustomScaledWait` per glyph. Activated glyphs pulse + burst then stay dimmed ("spent"); fizzles shake, grey out,
  pop a grey dust puff and linger faded before dissolving;
  at the end spent glyphs dissolve upward in sequence while Seal-retained glyphs stay.
- Glyphs that will outlast the next Speak are framed by a mandala in the rune's colour (a heptagon inside a pentagon, counter-rotating). It comes from a dry run of the Incantation (`RuneCmd.Forecast` → `RunePreview.Persisting`), so it covers Persist, Diminish (unless it would fizzle away), Growth and the glyph it grows, the glyph a Clone copies, everything after a Seal, and the whole Incantation under Eternal Sigil / Lingering Aroma.
- Co-op: other players' runes are faded; hovering another player brings theirs forward and fades yours.
- Rune values overhead show game effects (Strength, Weak, Vulnerable, Frail, Dexterity…) via `PayloadRune.Modified`
  (green = raised, red = lowered, like card text). The same dry run supplies them; refreshed on `CombatStateChanged` and
  whenever any player's Incantation changes.
- **Effects of earlier runes count**: a payload's `PreviewPowers` (Hex: Weak + Vulnerable) are applied to its known targets
  as detached power copies in `PreviewEffects`, and later runes preview against them (`[Hex 1][Strike 5]` shows Strike 7 on
  the same target). Targets follow the target runes before them (Anchor/Nova/Execution/Mirror); a rolled target (Scatter,
  dead anchor with several enemies) applies nothing. A rune hitting several creatures shows the value they all agree on,
  else the target-independent one. **Co-op**: players Speak in combat order, so `RuneCmd.Forecast` dry-runs every
  teammate before you first and carries their effects over (P1's Hex raises P2's Strike).
- **Hovering a glyph** shows its tips, puts the game's targeting reticle on every creature it would affect and streams an
  arc of rune-coloured motes to each (`NRuneArc`; fainter and sparser for rolled targets). Target runes highlight what
  their mode would pick (Execution → lowest HP).
- Fonts: only the game's bundled locale fonts are used; missing glyphs log a warning and render `◆`.
  To use scripts not covered (e.g. Elder Futhark runes ᚠᚢᚦ), ship an OFL font such as Noto Sans Runic in `RunePriest/`.
- Known gaps: placeholder particles are untextured squares; runes inscribed at end of turn (Mirrororrim) aren't previewed.

## 13. Phase 4 — Expansion
Decisions made autonomously (user unavailable; revisit on review):
- **Capacity is opt-in via content.** Default stays unlimited. `IRuneListener.ModifyCapacity` (smallest wins). When a glyph
  would exceed capacity it **Overflows**: the oldest glyph is Spoken alone, immediately (`RuneCmd.SpeakAt`), like Defect evoke.
  UI draws faint empty-slot rings when a capacity is active. No current content sets a capacity.
- **Curse runes** (Mirror, Blood) are engine-only since Phase 5; the Smudged/Stray Rune status/curse cards were removed with
  the prototype set. Re-add via `[Pool(typeof(StatusCardPool))]` + `AfterCardDrawn` if wanted.
- **Forecast**: the Incantation power tooltip shows a dry run (`RuneCmd.Forecast` → interpreter with `RunePreview`): totals
  per payload, fizzles, overload. Totals include game effects (`PayloadRune.Modified`, green/red vs base); a target's
  effects (Vulnerable) only count when the target is known without rolling, so preview never advances the combat RNG.
  Effects that runes earlier in the Speak (or a teammate's earlier Speak) will apply are included — see §12.
- New listener hooks: `ModifyLoopCount`, `ModifyCapacity`, `KeepsIncantation`, `AfterInscribed`, `AfterPayload`,
  `AfterFizzle`, `AfterSpeak` (with `RuneContext.GlyphsSpoken` / `Fizzles`). Value/loop/capacity hooks must be pure.
- New rune: **Sanctify** (modifier, ⊘). (Venom was added here and removed again in Phase 5.)
- New speak variants: `Speak(..., keep: true)`, `SpeakAt(index)` (Overflow).
- Phase 5 hooks: `ModifyInscription` (reshape a cast before it lands — Runic Form, Odd Sigil, Imbued Teacup), `AfterImbued`
  (Paladin Sigil), `AfterRemoved` (Swift Sigil); commands `RuneCmd.Imbue/ImbueAll/Remove/Transform`.

The Phase 4 prototype content (Chalk Line, Tight Script, Slate Tablet, Smudged/Stray Rune, …) was **removed** in Phase 5 in
favour of the designed set in §9. The engine features above (capacity/Overflow, curse runes, Forecast, listener hooks,
keep/SpeakAt) are still available for future content.

## 14. Co-op sharing and permanent Imbue (issue #14)
- **Choral Evocation** (`Cards/Rare/ChoralEvocation.cs`, `CardMultiplayerConstraint.MultiplayerOnly`, Exhaust) applies
  `ChoralEvocationPower` (upgraded: `ChoralEvocationPlusPower`) until the end of the turn. On `AfterInscribed`, the
  power copies what you just inscribed into every other living player's Incantation with `RuneCmd.Share`:
  unupgraded only the Defend runes of each glyph (`Glyph.Only`, so Quick Scribe shares `[Defend 3]`), upgraded every
  glyph as-is (modifiers, targets, loops and Blood included). Runes inscribed by playing an Imbued card count too.
- Shared copies **belong to the receiving player**: no source card (so a shared Strike is dealt by them and uses their
  Strength), same anchor, merged/Overflowed like an Inscribe, and Spoken with their Incantation. `Share` raises no
  inscription listeners, so two Choral Evocations can't bounce runes back and forth and a teammate's Flow State / Midas
  Hand don't trigger on your cards. Works for any character (the Incantation power is applied on demand).
- Assumptions: "inscribed this turn" means from the moment the card is played until the end of the turn (runes
  already in the Incantation aren't shared); if both versions are active only the upgraded one shares.
- **Aether Quill** (`Potions/AetherQuill.cs`, `PotionRarity.Event`, combat only): choose an Imbued card in hand that
  has a deck card; `MakeImbuePermanent` writes its `ImbuedRunes` to the deck card, which is saved with the run. Cards
  generated in combat have no deck card and can't be chosen.
- **Neow**: `NeowAetherQuillPatch` postfixes `Neow.GenerateInitialOptions`; for a Rune Priest in a regular (no
  modifier) run it replaces one of the two positive offers with the Aether Inkwell relic
  (`NeowAetherQuillPatch.Chance` = 25%, rolled with the event's own RNG so co-op and reloads agree). The relic grants
  the potion on pickup — Neow's offers are relic options, so the relic is the carrier.

## 15. Ancients and Ascended cards
Ancient boons are relics offered by the game's ancients. BaseLib covers Orobas and Darv's Dusty Tome; the rest are
Harmony postfixes (`AncientOptionPatches`) that swap a Rune Priest boon into one slot with roughly the odds of one more
relic in that slot's pool, rolled on the event's own RNG (deterministic for co-op and reloads).

| Ancient | Boon | How |
|---|---|---|
| Orobas | **Touch of Orobas** → Blessed Toolbox becomes **Hallowed Toolbox** | `BlessedToolbox.GetUpgradeReplacement` |
| Orobas | **Archaic Tooth** → your starting rune (the Common "… Rune" card Blessed Toolbox added) transforms into its Ascended form | `ITranscendenceCard` on each Common rune card |
| Darv | **Dusty Tome** → Mirrororrim (upgraded) | `ITomeCard` on `Mirrororrim` |
| Darv | **Dark Tablet** (25%, replaces an old relic, never Dusty Tome) | `AncientOptionPatches.DarvOptions` |
| Vakuu | **Corrupted Sigil** (25%, pool 2 slot) | `AncientOptionPatches.VakuuOptions` |
| Tezcatara | **Eternal Candle** (20%, pool 3 slot) | `AncientOptionPatches.TezcataraOptions` |

Archaic Tooth mapping (starting rune → Ascended card): Execution → Annihilation, Nova → Supernova, Hex → Curse,
Mending → Healer, Cracked → Disintegrate, Blank → Strategi, Echo → Cathedral, Heavy Rune → Light Blade,
Amplification → Multiply, Growth → Overgrowth, Diminishing → Atrophy.

Ancient cards (`Cards/Ancient`, `CardRarity.Ancient`: never in card rewards or random generation; full-art frame):

| Card | Type | Cost | Effect | Upgrade |
|---|---|---|---|---|
| Annihilation | Attack | 1 | Inscribe [Execution][Strike 15]×3. | Ascended: none |
| Supernova | Skill | 1 | Inscribe [Nova]. Remove every [Void] (`RuneCmd.Remove`). | Ascended: none |
| Curse | Skill | 1 | Inscribe [Hex 1] (Persist). | Ascended: none |
| Healer | Skill | 1 | This turn, every payload rune you inscribe gets a simultaneous [Mend 3] in the same glyph; targeting, empowering (modifier) and flow runes can't hold a payload, so they're skipped. | Ascended: none |
| Multiply | Skill | 1 | Inscribe [Twin ×2][Twin ×2]. | Ascended: none |
| Strategi | Skill | 0 | Imbue 1. Draw 3. | Ascended: none |
| Cathedral | Skill | 1 | Inscribe [Clone][Echo]. | Ascended: none |
| Light Blade | Attack | 0 | Inscribe [Strike 9 + 3 per rune in the Incantation]. | Ascended: none |
| Disintegrate | Attack | 1 | Inscribe [Amplify +10][Loop][Strike 2][Strike 2][End Loop]. | Ascended: none |
| Overgrowth | Skill | 0 | Inscribe [Overgrowth 3]. | Ascended: none |
| Atrophy | Attack | 2 | Inscribe [Diminish 80]. Exhaust. | Ascended: none |
| Eternal Scroll | Skill | 0 | Inscribe its etched runes. +1 cost per Etching (saved `EtchedRunes`/`Etchings`; cost = base + etchings − upgrades, min 0). | −1 cost, unlimited |
| Corrupted Sigil | Power | 0 | Cards with "Rune" in their name are Ethereal, Exhaust and cost 0; the Incantation is kept after Speaking. | not Ethereal |
| Mirrororrim | Power | 2 | Ethereal. End of turn (before the Speak, `BeforeSideTurnEndEarly`): Inscribe [End Loop][Reflection]. | not Ethereal |

Ascended cards (the first eleven) are a subset of Ancient cards with `MaxUpgradeLevel => 0`.

## 16. Events
See [custom-events.md](custom-events.md): Pulsing Pedestal (Act 1), Enchanted Forge (Act 2, forced; makes a Forged Rune), Suspicious Tailor (Act 3, Lightweight Cloth Robe). An event only enters the pool when every player has a valid target for it; rune-specific options are locked per player.
