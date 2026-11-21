# Cosmic Horizon (AMESR)

Unity 6 / URP 2D action-diving and cantina-management vertical slice.

## Run

1. Open the project in **Unity 6000.5.7f1**, with an activated Unity license.
2. Run **Cosmic Horizon → Build Vertical Slice** once. This authors ScriptableObjects, animation controller/clips, prefabs, canvases, nine scenes, and Build Settings entries from existing sprites.
3. Open **Assets/Scenes/SC_MainMenu.unity** and press **Play**.
4. Choose **New Game**. Mothership buttons lead to the tutorial, four dive zones, cantina, workshop, and bed. Catch ingredients before opening service. Complete service, continue to the workshop, then sleep to advance/save the day.

The builder preserves generated asset GUIDs but regenerates prefab content. Make custom prefab edits after building, or incorporate them into the builder before rebuilding.

Boss gates in dive zones 2–4 load SC_BossArena additively. Enter through a dive gate rather than playing the arena alone. Victory leaves a 12-second loot window before hub return.

Uncollected boss rewards remain in the saved reward ledger. Free cargo slots and click **Claim boss loot** at the mothership; arena pickups and hub claims share one-time receipts. Manual saves are hub/preparation/completed-night checkpoints only, not mid-dive or during active service. After pulling code changes, rerun the builder to update generated UI and event wiring.

## Controls

| Input | Action |
| --- | --- |
| WASD / arrow keys | Move; swim in all directions underwater |
| Space | Jump; release early for a shorter jump |
| Left Shift | Jetpack boost / grounded run modifier; consumes fuel and can overheat |
| Mouse | Aim |
| Left click | Fire; with harpoon, hold to reel and tap to increase tension |
| Right mouse, held | Grapple an anchor; release to preserve swing momentum |
| Q / R | Switch weapon / reload |
| 1–5 | Select harpoon tip |
| E, held near ore | Mine |
| E at cantina counter/table | Pick up / serve / clear plates |
| E or Space | Reveal/advance dialogue; completed lines auto-advance after three seconds |
| I | Inventory; drag to merge/swap; right-click for Use, Sell, Info |
| F1–F5 or quick-slot buttons | Use the corresponding consumable |
| Esc | Pause/resume |

Use and sell one item at a time. Selling is available at the mothership/cantina. New games include two medkits. Depth upgrades unlock deeper dive-pod buttons; oxygen, fuel, harpoon, armor, tray, seats, and cooking upgrades affect their associated systems.

## Optional audio

No audio files are shipped or fabricated. PF_AudioManager has optional arrays indexed by GameState, SfxType, and biome. Assign clips after building; missing entries are silent. Music uses half-second fade-out/fade-in ramps. Settings persist master/music/effects volume; resolution/fullscreen controls apply to the current player.

## Checks

From the repository root:

~~~sh
bash Tools/compile_check.sh
bash Tools/test_core.sh
python3 Tools/generate_metas.py
~~~

The compiler covers runtime **and Editor** C# using Unity's bundled Roslyn without an Editor license. Pure tests cover save validation, timing, oxygen, weighted selection, tension, damage, service/reputation/economy, boss thresholds, inventory, trauma, and quest progression. These checks do **not** execute Unity or prove playability.

## Section 7 audit

**V** = verified by source/count or pure test. **I** = implemented, Unity runtime/visual acceptance unverified. **B** = builder-authored deliverable, not generated/inspected on this unlicensed machine. **N** = not met.

| Checklist item | Status / evidence or remaining reason |
| --- | --- |
| 70+ organized C# scripts | **V** — 164 scripts: 155 runtime, 9 Editor |
| 15+ ScriptableObject data assets created | **B** — builder declares well over 15; generation requires a license |
| 14+ ScriptableObject event assets | **B** — builder-authored; generation unverified |
| Walk/run/jump, coyote time, buffering, variable jump, acceleration | **I** — PlayerController / PlayerStateMachine |
| Swim/dive and nonlinear oxygen drain | **I** — OxygenSystem; formula tested |
| Jetpack fuel and overheating | **I** — JetpackSystem |
| Harpoon aim/fire/hit/reel/tension | **I** — HarpoonSystem; tension math tested |
| Grapple attachment/swing/momentum | **I** — GrappleSystem / DistanceJoint2D |
| Hold-to-mine with progress bar | **I** — MiningSystem / OreNode |
| Five-plus creature species / varied AI | **I/B** — eight authored creature definitions, three AI implementations |
| Zero-GC schooling boids | **I** — preallocated FlockManager arrays; profiler measurement unavailable |
| Four depth-based spawning biomes | **I/B** — CreatureSpawner; 30-active cap with furthest recycling |
| Weighted loot drops | **I** — LootTable; selection math tested |
| Five functional weapon types | **I** — Harpoon, Blaster, Shotgun, Sniper, PlasmaBlade |
| Three multi-phase boss fights | **I/B** — Maw, Chrono Jellyfish, Void Leviathan (whale patterns/sprites) |
| Boss health bar with phases | **I/B** — BossHealthBarUI, slide-in, I/II/III indicators |
| Cantina FSM/patience/kitchen/service/tips | **I** — cantina systems; service math tested |
| Menu setup and night report | **I/B** — ingredient-backed menu; report/continue-to-workshop flow |
| Bronze–Diamond reputation | **I** — saved points; thresholds tested |
| Credits / exponential upgrade costs | **I** — saved transactions/gear; pricing math tested |
| Grid inventory / drag-and-drop | **I/B** — 20 slots, merge/swap, rarity, tooltip, contextual actions, five consumable slots |
| Shop purchases | **I/B** — eight upgrade cards, affordability/MAX handling, previews |
| Portrait/typewriter dialogue | **I/B** — five portrait conversation assets; hub crew conversation |
| Ten quest definitions | **B/I** — ten authored quests; event-driven progress, saved completion/rewards |
| Ten tutorial steps | **I/B** — movement through first service; saved step |
| Five-layer parallax per biome | **I/B** — three prebuilt copies per layer; infinite horizontal recycling |
| Camera dead zone/look-ahead/trauma | **I** — follow baseline, state zoom, quadratic trauma |
| Gravity ↔ zero-g water transition | **I** — WaterSurfaceTransition |
| JSON save/load | **I** — SaveLoadManager; validation tested, Unity JSON/file integration unverified |
| Main menu and pause menu | **I/B** — New/Continue, settings, save/load, navigation, pause time scale |
| Nine scenes created and linked | **B** — authored/linked; actual generation and traversal unverified |
| Pool all runtime-spawned entities | **V/I** — source audit finds Instantiate only in initialization-time ObjectPool.Prewarm; runtime/profiler validation unavailable |
| Utilize all 4,249+ sprites; no placeholders | **N** — selected existing art is referenced; many alternate frames, unused variants, sheets, and unrelated assets remain unused. No replacement artwork was invented. Literal utilization of every sprite is not implemented. |

### Acceptance still required

There is no activated Unity license here. Therefore **all I/B items require a licensed Editor build/playthrough**: builder rerun/idempotence, generated asset counts, UI layouts/interactions, collision/timing/boss balance, scene traversal, save/load integration, and profiling. No runtime, visual, performance, or production-readiness approval is claimed.

Optional radar minimap and optional dialogue choices are not implemented. Audio cannot be auditioned without real clips. The HUD uses numeric day/biome text rather than dedicated sun/moon artwork.
