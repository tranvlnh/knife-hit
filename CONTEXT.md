# Knife Hit

An offline knife-throwing game with a 30-level campaign in which the player embeds knives in a rotating target without hitting an existing knife, earns coins and stars, and unlocks knife skins.

## Language

**Campaign (Chiến dịch)**:
The ordered list of 30 levels, grouped into 3 worlds. Level N+1 opens once level N is won.
_Avoid_: Story, season.

**World (Thế giới)**:
A group of 10 consecutive levels sharing a background and tint. The last level of each world is a boss level.
_Avoid_: Chapter, zone.

**Level (Cấp độ)**:
One entry of the campaign, made of one or more stages in order. A normal level has one stage; a boss level has one stage per boss phase. Defines its coin reward and its star time limits.
_Avoid_: Stage when referring to a campaign entry.

**Run (Lượt chơi)**:
A single attempt at one level, starting at its first stage and ending when the level is won or an embedded knife is hit. Retry starts a new run of the same level. A terminated application does not retain the run.
_Avoid_: Round, match, session when referring to a player's attempt.

**Stage (Màn)**:
One challenge within a level, defined by its required knife count, initial embedded knives, and target rotation pattern.
_Avoid_: Wave, round, level.

**Boss (Trùm)**:
A level whose stages are phases of one encounter, with deterministic rules signalled in advance (see Guard and Wide Zone in `docs/knife-collision.md`). There are three: levels 10, 20 and 30; level 30 is the final boss.
_Avoid_: Enemy; boss for the standard target.

**Phase (Pha)**:
One stage of a boss level. Clearing a phase moves to the next phase through a short transition; the final phase wins the level.
_Avoid_: Wave, round.

**Target (Bia)**:
The rotating object into which the player throws knives.
_Avoid_: Enemy, boss for the standard target.

**Flying Knife (Dao đang bay)**:
The single thrown knife whose landing result has not yet been resolved. Further throw inputs are ignored while a flying knife exists.
_Avoid_: Queued knife.

**Embedded Knife (Dao đã cắm)**:
A knife attached to the target, either present at the start of a stage or added by a successful throw. Hitting an embedded knife ends the run.
_Avoid_: Flying knife when referring to an attached knife.

**Successful Throw (Lần ném thành công)**:
A throw that embeds a new knife in the target without hitting an embedded knife. The game keeps no score; progress is measured only by levels won and stars.
_Avoid_: Hit without specifying whether the knife hit the target or another knife; score, points.

**Star (Sao)**:
A rating of 1 to 3 awarded for winning a level, by the time spent in the run: 1 star for a win, 2 stars when won within the level's two-star time, 3 stars within its three-star time. Run time counts only seconds spent playing (not paused, in transitions or in menus) across all stages of the level. The best rating per level is kept; stars are not a score.
_Avoid_: Score, points, rank.

**Coin (Xu)**:
The in-game currency, earned only by winning levels and spent only on knife skins. A level's first win pays its coin reward; a later win pays only its star bonus for each star above the best previous rating. No real money or ads are involved.
_Avoid_: Gem, cash, money.

**Knife Skin (Dao)**:
A cosmetic knife appearance. It changes only the sprite and tint; the knife's size and collision geometry never change. The default skin is always owned. Skins are bought with coins or unlocked by winning a specific level.
_Avoid_: Weapon, upgrade.

**Paused Run (Lượt chơi tạm dừng)**:
A run suspended while the player leaves the application, with its target and flying knife held in place. Returning to the application requires an explicit Continue action before gameplay resumes.
_Avoid_: Saved run, restored run.
