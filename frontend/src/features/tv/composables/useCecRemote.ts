import { onMounted, onUnmounted } from "vue";

export interface CecActions {
  onPlay?: () => void;
  onPause?: () => void;
  onSkip?: () => void;
  onRestart?: () => void;
  onStop?: () => void;
  onToggleOverlay?: () => void;
  onNavigateUp?: () => void;
  onNavigateDown?: () => void;
}

/**
 * Maps keyboard events to CEC remote actions. The Pi kiosk receives
 * CEC button presses as keyboard events (via cec-client input driver
 * or xdotool integration). This composable catches them in the browser.
 *
 * Mapping follows the CEC → keyboard standard:
 * - Play/Pause  → MediaPlayPause / Space
 * - Right →     → ArrowRight
 * - Left ←      → ArrowLeft
 * - OK/Select   → Enter
 * - Back/Return → Backspace / Escape
 * - Up ↑        → ArrowUp
 * - Down ↓      → ArrowDown
 */
export function useCecRemote(actions: CecActions) {
  let lastKey = "";
  let lastTime = 0;
  const DEBOUNCE_MS = 300;

  function onKeyDown(e: KeyboardEvent) {
    const now = Date.now();
    if (e.key === lastKey && now - lastTime < DEBOUNCE_MS) return;
    lastKey = e.key;
    lastTime = now;

    switch (e.key) {
      case "MediaPlayPause":
        // Toggle: delegate to external handler
        actions.onPlay?.();
        e.preventDefault();
        break;
      case " ":
        actions.onToggleOverlay?.();
        e.preventDefault();
        break;
      case "Enter":
        actions.onToggleOverlay?.();
        e.preventDefault();
        break;
      case "ArrowRight":
        actions.onSkip?.();
        e.preventDefault();
        break;
      case "ArrowLeft":
        actions.onRestart?.();
        e.preventDefault();
        break;
      case "ArrowUp":
        actions.onNavigateUp?.();
        e.preventDefault();
        break;
      case "ArrowDown":
        actions.onNavigateDown?.();
        e.preventDefault();
        break;
      case "Backspace":
      case "Escape":
        actions.onStop?.();
        e.preventDefault();
        break;
    }
  }

  onMounted(() => {
    window.addEventListener("keydown", onKeyDown);
  });

  onUnmounted(() => {
    window.removeEventListener("keydown", onKeyDown);
  });
}
