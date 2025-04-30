# 🃏 Multiplayer Blackjack Brawl – Final Project Checklist

## ✅ Core Project Requirements (Multiplayer Features)

- [x] Connect two instances of the game using **Unity Multiplayer Center**.
- [x] Load a scene via the network (host-client setup).
- [x] Spawn and control a **player prefab** when the scene loads.
- [x] Create **multiple random spawn locations** for players.
- [x] Move the **local player** independently (no interference with others).
- [x] Sync a **camera** to follow the local player.
- [ ] Implement a "shooting" or **spawning behavior** (e.g., spawning cards or chips on action).
- [ ] Objects that **perform actions** based on **networked variables** (e.g., chip count, hand value, etc.).
- [ ] Demonstrate **networked collisions** (e.g., spawned cards/chips disappearing on pick-up).
- [ ] Modify a variable **based on collisions** or gameplay events.
- [ ] Create a **UI component** that tracks game events (e.g., wins/losses, chip count, power card use).
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
- [ ] Players draw from a **13-card deck** (standard cards + power cards).
- [ ] Players earn **Chips** by winning rounds or bonus plays.
- [ ] Each round has phases: **Deal**, **Action (Hit/Stand/Power Card)**, **Resolution**.

### HP Battle System (Add-On Features):

- [x] Implement **player HP** as a networked variable (e.g., 30 HP).
- [x] Calculate **damage** based on hand value difference after each round (including bust = 0, Blackjack bonus).
- [x] Apply **special damage rules** for busting or exact 21.
- [ ] Update **HP bar UI** for each player.
- [ ] Add **damage pop-ups** for visual feedback.
- [x] Eliminate players who reach **0 HP**.
- [x] Declare **last player standing** as winner (auto rounds until only one remains).
- [ ] Integrate HP-related **Power Cards**:
  - [ ] Heal Card, Vampire Card, Shield Wall.


### Power Cards:
- [ ] Implement **Card Swap** – swap one card with another player.
- [ ] Implement **Value Shift** – modify card by +1 or -1.
- [ ] Implement **Wildcard** – select 1 or 11 for a card.
- [ ] Implement at least **5 more unique power cards**:
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
- [ ] Players earn **Chips** based on performance per round.
- [ ] Chips can be used to:
  - [ ] **Buy Power Cards**.
  - [ ] **Remove cards** from their deck (deck slimming).
  
### Deck System:
- [ ] Players start with a **13-card deck**.
- [ ] Deck updates **between rounds** via simple **shop**:
  - [ ] Buy new power cards (adds to deck).
  - [ ] Pay chips to remove cards.
  
---

## 🧩 UI Elements:
- [x] Debug multiplayer UI with turn-based controls and state display (for development)
- [ ] Blackjack Table UI (player hand, deck, chips, power card buttons).
- [ ] **Chip Counter** (networked variable display).
- [ ] **Power Card Button Bar** (click to activate).
- [ ] **Event Tracker UI** (track wins, chips, power cards used).
- [ ] Chat UI and Player List UI.

---

## ⚙️ Stretch Goals (Optional but Cool):
- [ ] Add more **Power Cards** for variety.
- [ ] Add basic **Deck Theme bonuses** (Lean Deck, Power Deck etc.).
- [ ] Implement **bonus conditions** (Five Card Charlie, Suited Win, etc.).
- [ ] Add sound effects or animations for card play and round wins.
