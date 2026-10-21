# Knife Hit

An offline knife-throwing game in which the player completes five sequential stages by embedding knives in a rotating target without hitting an existing knife.

## Language

**Run (Lượt chơi)**:
A single attempt starting at stage 1 and ending in failure or completion of stage 5. A new run always starts at stage 1; a terminated application does not retain the run.
_Avoid_: Round, match, session when referring to a player's attempt.

**Stage (Màn)**:
One of five ordered challenges within a run, defined by its required knife count, initial embedded knives, and target rotation pattern.
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
A throw that embeds a new knife in the target without hitting an embedded knife. Each successful throw adds one point to the run score.
_Avoid_: Hit without specifying whether the knife hit the target or another knife.

**Run Score (Điểm lượt chơi)**:
The number of successful throws accumulated across stages in the current run. It resets to zero when a new run starts.
_Avoid_: Stage score, currency.

**High Score (Điểm cao)**:
The highest run score achieved on the device, retained between runs and application launches.
_Avoid_: Leaderboard, total lifetime score.

**Paused Run (Lượt chơi tạm dừng)**:
A run suspended while the player leaves the application, with its target and flying knife held in place. Returning to the application requires an explicit Continue action before gameplay resumes.
_Avoid_: Saved run, restored run.
