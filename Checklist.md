# 🃏 Multiplayer Blackjack Brawl – Final Project Checklist

## ✅ Core Project Requirements (Multiplayer Features)

- [x] Connect two instances of the game using **Unity Multiplayer Center**.
- [x] Load a scene via the network (host-client setup).
- [x] Spawn and control a **player prefab** when the scene loads.
- [x] Create **multiple random spawn locations** for players.
- [x] Move the **local player** independently (no interference with others).
- [x] Sync a **camera** to follow the local player.
- [ ] Implement a "shooting" or **spawning behavior** (e.g., spawning cards or chips on action).
    - [ ] Design the object to be spawned (card/chip prefab)
    - [ ] Implement local spawn logic on player action (e.g., button press)
    - [ ] Network the spawn event so all players see the object
    - [ ] Add visual and audio feedback for spawning
    - [ ] Despawn or clean up objects as needed
- [ ] Objects that **perform actions** based on **networked variables** (e.g., chip count, hand value, etc.).
    - [ ] Identify which variables should drive object behavior
    - [ ] Implement logic for objects to react to variable changes (e.g., chips update when hand value changes)
    - [ ] Sync state changes across network
    - [ ] Test object reactions in multiplayer
- [ ] Demonstrate **networked collisions** (e.g., spawned cards/chips disappearing on pick-up).
    - [ ] Add collider and networked component to spawned objects
    - [ ] Detect collision with player or other objects
    - [ ] Network the collision event (e.g., object picked up/disappears for all)
    - [ ] Update player state/variables on collision
- [ ] Modify a variable **based on collisions** or gameplay events.
    - [ ] Decide which variables are affected (e.g., chip count, HP)
    - [ ] Implement variable change logic in collision/event handler
    - [ ] Ensure variable changes are networked
    - [ ] Add UI feedback for variable changes
- [ ] Create a **UI component** that tracks game events (e.g., wins/losses, chip count, power card use).
    - [ ] Design layout for event tracker UI
    - [ ] Implement event logging (e.g., win/loss, chip gain/loss, power card used)
    - [ ] Display event log in UI
    - [ ] Sync event tracker UI across network
- [x] Demonstrate **Unity Cloud** interaction (Relay, Lobby, or Authentication).
- [x] Implement a **Chat System**.
- [x] Implement a **Player List** system (show connected players).

---

## 🎮 Core Game Features (Blackjack + Balatro Elements)

### Blackjack Core Loop:
- [x] Implement **Blackjack rules** (goal: 21 or closest under).  
    - [x] Basic multiplayer turn-based Blackjack loop (deal, hit/stand, PvP winner, no dealer)
    - [x] Game is now fully PvP (dealer logic removed)
    - [ ] Power cards, chips, and advanced rules (in progress)
    - [ ] Design power card types and effects
    - [ ] Implement power card logic in game flow
    - [ ] Add chip earning/spending logic
    - [ ] Integrate advanced rules (e.g., special win conditions)
    - [ ] Update UI to support new features
- [ ] Players draw from a **13-card deck** (standard cards + power cards).
    - [ ] Define deck structure (standard + power cards)
    - [ ] Implement deck initialization and shuffling
    - [ ] Ensure deck syncs correctly in multiplayer
    - [ ] Add UI to show remaining cards/deck composition
- [x] Players earn **Chips** by winning rounds or bonus plays.
    - [x] Define chip reward logic for wins/bonuses
    - [x] Implement chip tracking per player
    - [x] Network chip updates
    - [x] Update UI to display chip count
- [x] Each round has phases: **Deal**, **Action (Hit/Stand/Power Card)**, **Resolution**.
    - [x] Implement clear phase transitions in code
    - [x] Display phase to players in UI
    - [x] Handle player input/actions per phase
    - [x] Sync phase state across network

### HP Battle System (Add-On Features):

- [x] Implement **player HP** as a networked variable (e.g., 30 HP).
- [x] Calculate **damage** based on hand value difference after each round (including bust = 0, Blackjack bonus).
- [x] Apply **special damage rules** for busting or exact 21.
- [ ] Update **HP bar UI** for each player.
    - [ ] Design HP bar UI element
    - [ ] Bind HP bar to networked player HP variable
    - [ ] Update HP bar in real-time
    - [ ] Test HP bar with multiple players
- [ ] Add **damage pop-ups** for visual feedback.
    - [ ] Design pop-up visual (animation/text)
    - [ ] Trigger pop-up on damage event
    - [ ] Network pop-up so all players see it
    - [ ] Integrate with HP/damage system
- [x] Eliminate players who reach **0 HP**.
- [x] Declare **last player standing** as winner (auto rounds until only one remains).
- [ ] Integrate HP-related **Power Cards**:
    - [ ] Implement Heal Card effect (restore HP)
    - [ ] Implement Vampire Card effect (steal HP)
    - [ ] Implement Shield Wall effect (block damage)
    - [ ] Add UI and logic for using these cards


### Power Cards:
- [ ] Implement **Card Swap** – swap one card with another player.
    - [ ] Define swap logic and UI
    - [ ] Network the swap action
    - [ ] Update hands and UI for all players
- [ ] Implement **Value Shift** – modify card by +1 or -1.
    - [ ] Allow player to select a card to shift
    - [ ] Apply shift and network the change
    - [ ] Update UI to show new value
- [ ] Implement **Wildcard** – select 1 or 11 for a card.
    - [ ] Allow player to choose value when playing Wildcard
    - [ ] Apply value and network the choice
    - [ ] Update UI to reflect Wildcard
- [ ] Implement at least **5 more unique power cards**:
    - [ ] Define each power card's effect and rules
    - [ ] Implement logic for each card
    - [ ] Add UI and feedback for card use
    - [ ] Test each card in multiplayer
  - [ ] Negative Card – Add a -2 value card to reduce your total.
  - [ ] Raise Bust Limit – Bust at 24 this round.
  - [ ] Nullify – One card doesn’t count towards your total.
  - [ ] Forced Hit – Force another player to draw.
  - [ ] Randomizer – Randomize all your card values.
  - [ ] **Siphon** – Deal **3 damage** to a player, heal **3 HP** yourself. *(1 use per round)*
  - [ ] **Reckless Strike** – Deal **5 damage** to an opponent, take **2 self-damage**. *(1 use per game)*
  - [ ] **Punisher** – If an opponent **busts**, deal **+5 damage** to them. *(Passive, lasts 1 round)*
  - [ ] **Blood Pact** – Sacrifice **5 HP** to add **+5** to your hand total before bust check. *(1 use per game)*
  - [ ] **Mark of Pain** – Target a player; they take **2 damage** for each card they draw this round. *(1 use per round)*
### Chip Economy:
- [x] Players earn **Chips** based on performance per round.
    - [x] Define chip earning criteria
    - [x] Implement chip distribution after each round
    - [x] Network chip updates
    - [x] Update UI to reflect chip changes
- [ ] Chips can be used to:
    - [ ] Implement shop UI for buying/removing cards
    - [ ] Deduct chips on purchase/removal
    - [ ] Network chip and deck changes
    - [ ] Update UI for transactions
  
### Deck System:
- [ ] Players start with a **13-card deck**.
    - [ ] Initialize deck at game start
    - [ ] Ensure deck syncs for all players
    - [ ] Show deck contents in UI
- [ ] Deck updates **between rounds** via simple **shop**:
    - [ ] Implement shop logic for deck updates
    - [ ] Add/remove cards as needed
    - [ ] Network deck changes
    - [ ] Update UI after shop actions
  
---

## 🧩 UI Elements:
- [x] Debug multiplayer UI with turn-based controls and state display (for development)
- [ ] Blackjack Table UI (player hand, deck, chips, power card buttons).
    - [ ] Design and layout table UI
    - [ ] Bind UI to networked variables
    - [ ] Add interactivity for card and chip actions
    - [ ] Test UI with multiple players
- [ ] **Chip Counter** (networked variable display).
    - [ ] Add chip counter to UI
    - [ ] Bind to networked chip variable
    - [ ] Update in real-time
- [ ] **Power Card Button Bar** (click to activate).
    - [ ] Design button bar UI
    - [ ] Connect buttons to power card logic
    - [ ] Update UI when cards are used
- [ ] **Event Tracker UI** (track wins, chips, power cards used).
    - [ ] Log events in code
    - [ ] Display events in UI
    - [ ] Sync event log across network
- [ ] Chat UI and Player List UI.
    - [ ] Refine chat UI layout and features
    - [ ] Improve player list display
    - [ ] Ensure both update in real-time

---

## ⚙️ Stretch Goals (Optional but Cool):
- [ ] Add more **Power Cards** for variety.
- [ ] Add basic **Deck Theme bonuses** (Lean Deck, Power Deck etc.).
- [ ] Implement **bonus conditions** (Five Card Charlie, Suited Win, etc.).
- [ ] Add sound effects or animations for card play and round wins.
