// Generates the "sim-*" replay fixtures with the Pokemon Showdown simulator.
// Usage: npm install pokemon-showdown && node generate-simulated-replays.js <output directory>
const fs = require('fs');
const path = require('path');
const {Battle} = require('pokemon-showdown/dist/sim/battle');
const {Teams} = require('pokemon-showdown');
const {Utils} = require('pokemon-showdown/dist/lib');

// Replays show the public ("spectator") side of split messages and no debug output
function spectatorLog(battle) {
  const lines = [];
  for (let i = 0; i < battle.log.length; i++) {
    const line = battle.log[i];
    if (line.startsWith('|split|')) {
      lines.push(battle.log[i + 2]);
      i += 2;
    } else if (!line.startsWith('|debug|') && !line.startsWith('|t:|')) {
      lines.push(line);
    }
  }
  return lines;
}

// Mirrors the "!showteam" / "!showset" output of server/chat-commands/core.ts
// setNumber null: "!showteam", otherwise "!showset <setNumber>"
function postedSets(user, team, setNumber) {
  const showAll = setNumber === null;
  let html = Utils.escapeHTML(Teams.export(showAll ? team : [team[setNumber - 1]]));
  if (showAll) html = `<details><summary>View team</summary>${html}</details>`;
  return [`|c| ${user}|${showAll ? '!showteam' : `!showset ${setNumber}`}`, `|c| ${user}|/raw <div class="infobox">${html}</div>`];
}

function simulate({id, formatid, format, p1, p2, choices, chat = []}) {
  const battle = new Battle({formatid, seed: [1, 2, 3, 4]});
  const p1team = Teams.import(p1);
  const p2team = Teams.import(p2);
  battle.setPlayer('p1', {name: 'Alice', team: p1team});
  battle.setPlayer('p2', {name: 'Bob', team: p2team});
  for (const [p1choice, p2choice] of choices) {
    if (p1choice) battle.choose('p1', p1choice);
    if (p2choice) battle.choose('p2', p2choice);
  }
  const log = spectatorLog(battle);
  for (const [user, team, setNumber] of chat) {
    log.push(...postedSets(user, team === 'p1' ? p1team : p2team, setNumber));
  }
  return {id, format, formatid, players: ['Alice', 'Bob'], log: log.join('\n') + '\n', uploadtime: 0, rating: null, private: 0, password: null};
}

const scenarios = [
  {
    // Moves revealed by "cant" (Taunt, Queenly Majesty) and a set posted with "!showset"
    id: 'sim-gen9-cant-showset',
    formatid: 'gen9customgame',
    format: '[Gen 9] Custom Game',
    p1: `Taunter (Grimmsnarl) @ Light Clay
Ability: Prankster
- Taunt
- Spirit Break

Bob's Bane (Dragonite) @ Leftovers
Ability: Multiscale
- Extreme Speed
- Dragon Dance
- Earthquake
- Roost`,
    p2: `Tsareena @ Life Orb
Ability: Queenly Majesty
- Swords Dance
- Power Whip

Clefable @ Leftovers
Ability: Magic Guard
- Calm Mind
- Moonblast`,
    choices: [['team 12', 'team 21'], ['move 1', 'move 1'], ['move 1', 'move 1'], ['switch 2', 'switch 2'], ['move 1', 'move 1']],
    chat: [['Alice', 'p1', 2]],
  },
  {
    // Imposter and the move Transform copy the moves of the target
    id: 'sim-gen9-transform',
    formatid: 'gen9customgame',
    format: '[Gen 9] Custom Game',
    p1: `Ditto @ Choice Scarf
Ability: Imposter
- Transform

Mew @ Leftovers
Ability: Synchronize
- Transform
- Soft-Boiled`,
    p2: `Snorlax @ Leftovers
Ability: Thick Fat
- Curse
- Rest`,
    choices: [['team 12', 'team 1'], ['move 1', 'move 1'], ['switch 2', 'move 1'], ['move 1', 'move 1'], ['move 1', 'move 1']],
  },
  {
    // Zoroark disguised as Hypno uses Nasty Plot, Hypno reveals a move with Forewarn
    id: 'sim-gen9-illusion-forewarn',
    formatid: 'gen9customgame',
    format: '[Gen 9] Custom Game',
    p1: `Blissey @ Leftovers
Ability: Natural Cure
- Seismic Toss
- Soft-Boiled`,
    p2: `Zoroark @ Focus Sash
Ability: Illusion
- Nasty Plot
- Night Daze

Hypno @ Leftovers
Ability: Forewarn
- Toxic
- Psychic`,
    choices: [['team 1', 'team 12'], ['move 1', 'move 1'], ['move 2', 'switch 2'], ['move 2', 'move 1']],
  },
  {
    // Moves called by Assist, Copycat and Magic Coat, a Z-powered status move
    id: 'sim-gen7-called-moves-zstatus',
    formatid: 'gen7customgame',
    format: '[Gen 7] Custom Game',
    p1: `Liepard @ Leftovers
Ability: Prankster
- Assist
- Copycat
- Magic Coat

Kommo-o @ Leftovers
Ability: Bulletproof
- Swords Dance
- Close Combat

Breloom @ Grassium Z
Ability: Technician
- Spore
- Mach Punch`,
    p2: `Blissey @ Leftovers
Ability: Natural Cure
- Toxic
- Calm Mind`,
    choices: [['team 123', 'team 1'], ['move 1', 'move 2'], ['move 2', 'move 2'], ['move 3', 'move 1'], ['switch 3', 'move 2'], ['move 1 zmove', 'move 2']],
  },
  {
    // Max Moves are no moves of the set
    id: 'sim-gen8-dynamax',
    formatid: 'gen8customgame',
    format: '[Gen 8] Custom Game',
    p1: `Charizard @ Life Orb
Ability: Solar Power
Gigantamax: Yes
- Air Slash
- Protect`,
    p2: `Blissey @ Leftovers
Ability: Natural Cure
- Soft-Boiled
- Calm Mind`,
    choices: [['team 1', 'team 1'], ['move 2 dynamax', 'move 2'], ['move 1', 'move 2'], ['move 1', 'move 2'], ['move 1', 'move 2']],
  },
];

const outputDirectory = process.argv[2] || '.';
for (const scenario of scenarios) {
  const replay = simulate(scenario);
  fs.writeFileSync(path.join(outputDirectory, `${replay.id}.json`), JSON.stringify(replay));
  console.log(`--- ${replay.id}\n${replay.log}`);
}
