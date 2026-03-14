# MEDIA-722: TV On-Screen Keyboard & YouTube Search

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-720 (TV frontend), MEDIA-721 (CEC remote), MEDIA-710 (search via Invidious)

---

## Summary

Build a CEC-navigable on-screen keyboard on the TV that lets users search YouTube directly from the TV remote. Uses the same Invidious API as the mobile search (MEDIA-710) but with a TV-optimized grid layout navigable by arrow keys and OK button.

---

## TV Search Flow

```
1. User presses OK on remote while idle (or a dedicated search trigger)
2. Search overlay appears with on-screen keyboard
3. User navigates keyboard with ← → ↑ ↓ arrows
4. OK selects a letter → appends to search query
5. Results appear above keyboard as user types
6. User navigates to a result → OK to add to queue
7. Back button closes search and returns to player
```

---

## Screen Layout

```
┌──────────────────────────────────────────┐
│  🔍 Search: bohemian rha█               │
│                                          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │░░░░░░░░░│ │░░░░░░░░░│ │░░░░░░░░░│   │
│  │ Bohemian│ │ Bohemian│ │ Somebody│   │
│  │ Rhapsody│ │ Rhapsody│ │ to Love │   │
│  │ Queen   │ │ (Live)  │ │ Queen   │   │
│  │ ▶ 5:55  │ │ ▶ 5:39  │ │ ▶ 4:56  │   │
│  └─────────┘ └─────────┘ └─────────┘   │
│                                          │
│  [Q][W][E][R][T][Y][U][I][O][P]         │
│  [A][S][D][F][G][H][J][K][L][']         │
│  [⇧][Z][X][C][V][B][N][M][⌫]           │
│  [123] [________SPACE________] [Search]  │
└──────────────────────────────────────────┘
```

---

## Keyboard Component

### Grid-Based Navigation

The keyboard is a 2D grid. CEC arrow keys move a "focus cursor" between keys:

```javascript
class OnScreenKeyboard {
  constructor() {
    this.layouts = {
      alpha: [
        ['Q','W','E','R','T','Y','U','I','O','P'],
        ['A','S','D','F','G','H','J','K','L',"'"],
        ['⇧','Z','X','C','V','B','N','M','⌫'],
        ['123','SPACE','Search'],
      ],
      numeric: [
        ['1','2','3','4','5','6','7','8','9','0'],
        ['-','=','.',',','!','?','@','#','⌫'],
        ['ABC','SPACE','Search'],
      ],
    }
    this.currentLayout = 'alpha'
    this.focusRow = 0
    this.focusCol = 0
    this.isShift = false
    this.query = ''
  }

  navigate(direction) {
    const layout = this.layouts[this.currentLayout]
    switch (direction) {
      case 'up':
        this.focusRow = Math.max(0, this.focusRow - 1)
        // Clamp column to row length
        this.focusCol = Math.min(this.focusCol, layout[this.focusRow].length - 1)
        break
      case 'down':
        this.focusRow = Math.min(layout.length - 1, this.focusRow + 1)
        this.focusCol = Math.min(this.focusCol, layout[this.focusRow].length - 1)
        break
      case 'left':
        this.focusCol = Math.max(0, this.focusCol - 1)
        break
      case 'right':
        this.focusCol = Math.min(
          this.layouts[this.currentLayout][this.focusRow].length - 1,
          this.focusCol + 1
        )
        break
    }
    this.render()
  }

  select() {
    const key = this.layouts[this.currentLayout][this.focusRow][this.focusCol]

    switch (key) {
      case '⌫':
        this.query = this.query.slice(0, -1)
        break
      case 'SPACE':
        this.query += ' '
        break
      case '⇧':
        this.isShift = !this.isShift
        break
      case 'Search':
        this.performSearch()
        break
      case '123':
        this.currentLayout = 'numeric'
        this.focusRow = 0
        break
      case 'ABC':
        this.currentLayout = 'alpha'
        this.focusRow = 0
        break
      default:
        this.query += this.isShift ? key : key.toLowerCase()
        if (this.isShift) this.isShift = false
        break
    }

    this.render()
    this.autoSearch() // Debounced search as user types
  }
}
```

### Focus Styling

```css
.keyboard-key {
  background: rgba(255, 255, 255, 0.05);
  border: 2px solid transparent;
  border-radius: 8px;
  padding: 12px 16px;
  color: #e0e0e0;
  font-size: 24px;
  font-family: 'JetBrains Mono', monospace;
  transition: all 0.15s ease;
}

.keyboard-key.focused {
  background: rgba(255, 51, 102, 0.2);
  border-color: #ff3366;
  transform: scale(1.1);
  box-shadow: 0 0 20px rgba(255, 51, 102, 0.3);
}

.keyboard-key.special {
  background: rgba(255, 255, 255, 0.1);
  min-width: 80px;
}
```

---

## Search Results

Results appear in a horizontal scrollable row above the keyboard:

```javascript
async autoSearch() {
  // Debounce 500ms (longer than mobile — TV typing is slower)
  clearTimeout(this.searchTimer)
  if (this.query.length < 2) return

  this.searchTimer = setTimeout(async () => {
    const results = await searchInvidious(this.query)
    this.renderResults(results.slice(0, 5)) // Max 5 on TV
  }, 500)
}
```

### Result Card (TV-sized)

```
┌──────────────────┐
│ ░░░░░░░░░░░░░░░░ │  ← Thumbnail (320x180)
│ ░░░░░░░░░░░░░░░░ │
│ Song Title That   │
│ Might Be Long... │
│ Channel • 3:42   │
└──────────────────┘
```

- Focused result has accent border (same as keyboard focus)
- **Navigation**: When focus is in the results row, Left/Right moves between results, Down goes back to keyboard
- **OK on result**: Adds to queue via `POST /queue/add` and shows confirmation toast

---

## Navigation Zones

The search UI has 3 zones:

```
Zone 1: Search input (read-only display)
Zone 2: Results row (horizontal)
Zone 3: Keyboard grid (2D)
```

Up/Down moves between zones. The focus starts in Zone 3 (keyboard).

```javascript
navigate(direction) {
  if (direction === 'up' && this.currentZone === 'keyboard' && this.focusRow === 0) {
    // Move to results zone
    this.currentZone = 'results'
    this.resultFocusIndex = 0
    return
  }
  if (direction === 'down' && this.currentZone === 'results') {
    // Move back to keyboard
    this.currentZone = 'keyboard'
    return
  }
  // Normal navigation within zone
}
```

---

## Entry/Exit

- **Enter search**: OK button when in idle state, OR a dedicated "Search" option in the overlay menu
- **Exit search**: Back/Return button on remote
- **After adding song**: Brief "Added to queue ✓" toast, stay in search (user might add more)

---

## Tasks

- [ ] Build on-screen keyboard grid component
- [ ] Implement 2D grid navigation with focus tracking
- [ ] Implement key selection (letters, backspace, space, shift, layout switch)
- [ ] Integrate Invidious search API (same as MEDIA-710)
- [ ] Implement result cards with TV-friendly size
- [ ] Implement zone navigation (keyboard ↔ results)
- [ ] Add auto-search with 500ms debounce
- [ ] Add "Added to queue" confirmation toast
- [ ] Style for 1080p readability (large fonts, high contrast)
- [ ] Wire CEC events from WebSocket bridge (MEDIA-721)
- [ ] Test with CEC remote navigation
- [ ] Test on Pi 4 Chromium performance

---

## Acceptance Criteria

- [ ] On-screen keyboard navigable with arrow keys
- [ ] OK selects letters and types into search input
- [ ] Search results appear after 2+ characters typed
- [ ] Navigating to a result and pressing OK adds it to queue
- [ ] Back button closes search overlay
- [ ] Backspace key removes last character
- [ ] Space bar adds space to query
- [ ] Shift toggles uppercase for one letter
- [ ] 123/ABC switches between letter and number layouts
- [ ] Focus indicator clearly visible on TV (accent color + scale)
- [ ] Responsive to rapid key presses (debounced but not sluggish)
