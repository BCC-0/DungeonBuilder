- Game feel:
    - Should feel light and breezy
        - Kind of toon-like
        - No realistic death/pain depictions
        

### Core Gameplay Loop

- Crawl (already made) dungeon → Unlock parts → Build dungeons → Test/Play → Repeat
- Why is this loop fun?
    - Pros:
        - Solving puzzles made by someone else
        - Being creative with dungeons and animal mixes
        - Collecting all parts
        - 100% completing the set dungeons
        - 
    - Cons:
        - Needs a smooth and fun dungeon building program!
        - Needs consistent artstyle/enemy design for mixing idea
- What makes our version unique?
    - All enemies/bosses are mixes:
        - The head has it’s own unique attack against the player
        - The body has it’s own movement type and speed
    - Light/simple humor and story without any background

### Player Actions (PC / Mobile)

**Crawler Mode**

- Move:
    - WASD / Joystick
- Combat:
    - LMB / Left Button
- Interaction:
    - E / Clicking
- Item:
    - RMB / Right Button

**Builder Mode**

- Place:
    - Select placable from bar
    - LMB / Click on grid
- Modify:
    - RMB / hold certain block
    - Shows options this grid: (examples)
        - Rotate
        - Connect to puzzle component (for doors or other movables)
- Test:
    - A big play and stop button topleft should activate crawler mode
    - Should simply work like normal crawler mode until stop button is clicked

---

## 1. WORLD & DUNGEON STRUCTURE

### Dungeon Format

- Grid-based
- Rooms of any format → Builder should check if rooms are closed of!
- Doors separating rooms load new room and deload old room (for mobile performance)

**Reasoning:**

- Grid based is well-known and easy to work with
- Not having standard rooms will give extra freedom

### Dungeon Components

Describe the "language" of your dungeon.

- Rooms:
    - All rooms should be closed off by walls and/or doors
    - Bombable walls count as doors
- Tiles:
    - Player can place chosen tiles to replace the empty (black) grid
    - Should have collections of tiles belonging together
    - Water → If player wants to walk on it, it acts like a wall, but arrows fly over it etc.
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
        - Reference properties: When setting this, we go into select mode, wheere we must select another object in the dungeon. Objects we can select will be outgrined with a green dotted line.
        - Variable properties: When setting this, we only need to fill in the variable value, e.g. seconds for a timer or damage/health for an enemy.
- Themes / Biomes:
    - Farm theme
    - Forest theme
    - Underwater theme
    - Icy caves theme
    - Desert theme

---

## 2. CRAWLER MODE (Playable Core)

### Weapons/tools:

- Weapon ideas per theme:

### Enemies:

- Enemy ideas per theme:

---

## 3. BUILDER MODE (Creation Tools)

### Builder Actions

- Place
- Delete
- Undo / Redo
- Test dungeon

### 🧱 Buildable Parts

| Part | Category | Unlock Condition | Cost | Notes |
| --- | --- | --- | --- | --- |

---

## 4. PROGRESSION & UNLOCKS

### Player Progression

- What carries over between runs?
    - In the story, absolutely nothing. Levels must be self-contained to make every level it’s own adventure.
- How fast do players unlock tools?
    - Depends on the level, they could get them in the first room, or have to fight 20 enemies to find them!
- How do players unlock building blocks?
    - Every level should unlock a new building block, it can be small, it can be big. At the end of the story of each theme, the player should have everything of it’s theme!

---

## 5. ART DIRECTION

### Visual Style

- Style: Pixelart 16x16 per tile.
- Color rules: All outlines of enemies should be #303030 to give it a soft line and keep it consistent.
- Readability rules: White pixel art text with outline in UI.

### Environment Look

 Different per theme, but for farm theme:

- Walls: Red wood (like a barn). Normal wood.
- Floors: Hay, grass, wood.
- Props: Wheelbarrow with hay, pitchfork standing against the wall, fences, etc.
- Lighting mood: Overall, it should be lit everywhere (for now). We can add some simple lanterns/torches for atmosphere, but it should have no impact on the game.

---

## 6. UI / UX DESIGN

### Builder UI

- Placement controls:
- Menus:
- Feedback:

### Crawler HUD

- Health/energy: (Is the same to keep the player simpler.)
- Map:
- Alerts:

### Menus

- Main menu:
- Mode switching:
- Save / Load:

---

## 7. SOUND & FEEL

### Sound Design

- Player actions:
- Enemies:
- Traps:
- UI sounds:

### Game Feel

- Screen shake:
- Hit stop:
- Particles:
- Animations:

Sub-pages:

Enemy ideas per theme:

Weapon ideas per theme:

Tool ideas per theme:
