### Core Gameplay Loop

- Crawl (already made) dungeon → Unlock parts → Build dungeons → Test/Play → Repeat
    - Pros:
        - Solving puzzles made by someone else
        - Being creative with dungeons and animal mixes
        - Collecting all parts
        - 100% completing the set dungeons
    - Cons:
        - Needs a smooth and fun dungeon building program!
        - Needs consistent artstyle/enemy design for mixing idea
- What makes our version unique?
    - All enemies/bosses are mixes:
        - The head has it’s own unique attack against the player
        - The body has it’s own movement type and speed
    - Light/simple humor and story without any background

---

## 1. WORLD & DUNGEON STRUCTURE

### Dungeon Components
- Tiles:
    - Player can place chosen tiles to replace the empty (black) grid
    - Should have collections of tiles belonging together
    - Water -> If player wants to walk on it, it acts like a wall, but arrows fly over it etc.
- Connections:
    - Buttons
    - Levers
    - Keys/doors
    - Items with their own components!
- Traps:
    - Pits
    - Spikes
    - Lava
- Enemies (placing logic in dungeons):
    - Only place enemy → Stays dead after killed
    - Place enemy spawner → Gets revived everytime the player enters room
    - Place timed enemy spawner → Spawns the enemy every set seconds
- Triggers / Logic:
    - We create a UI that can take as many scrollable properties in a window as we want
    - We can edit tiles/enemies:
        - Each placable should have their own adjustable properties
        - Those properties should be accessible when modifying tile.
        - MUST SET properties: If those aren’t set, we can’t playtest or save!
        - COULD SET properties: If those aren’t set, this behaviour simply does nothing
        - Reference properties: When setting this, we go into select mode, where we must select another object in the dungeon.
        - Variable properties: When setting this, we only need to fill in the variable value, e.g. seconds for a timer or damage/health for an enemy.
- Themes / Biomes:
    - Farm theme
    - Forest theme
    - Underwater theme
    - Icy caves theme
    - Desert theme

---

## 2. CRAWLER MODE

### Weapons/tools:

- Weapon ideas per theme:

### Enemies:

- Enemy ideas per theme:

---

## 3. BUILDER MODE

### Builder Actions

- Place
- Delete
- Copy/paste
- Undo / Redo
- Test dungeon
---

## 4. ART DIRECTION

### Visual Style

- Style: Pixelart 32x32 per tile.
- Color rules: All outlines of enemies should be #303030 to give it a soft line and keep it consistent.
- Readability rules: White pixel art text with outline in UI.

### Environment Look

 Different per theme, but for farm theme:

- Walls: Red wood (like a barn). Normal wood.
- Floors: Hay, grass, wood.
- Props: Wheelbarrow with hay, pitchfork standing against the wall, fences, etc.
- Lighting mood: Overall, it should be lit everywhere (for now). We can add some simple lanterns/torches for atmosphere, but it should have no impact on the game.
