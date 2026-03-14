# MEDIA-705: PWA Configuration

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup)

---

## Summary

Configure VitePWA to make the admin/user frontend installable as a Progressive Web App. Mobile users can "Add to Home Screen" for an app-like experience with offline shell caching and a proper splash screen.

---

## VitePWA Configuration

```typescript
// vite.config.ts
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon.svg", "apple-touch-icon.png"],
      manifest: {
        name: "Media Platform",
        short_name: "Media",
        description: "Queue-based media playback controller",
        theme_color: "#0a0a0f",
        background_color: "#0a0a0f",
        display: "standalone",
        orientation: "portrait",
        start_url: "/",
        icons: [
          { src: "/pwa-192.png", sizes: "192x192", type: "image/png" },
          { src: "/pwa-512.png", sizes: "512x512", type: "image/png" },
          {
            src: "/pwa-512.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "maskable",
          },
        ],
      },
      workbox: {
        globPatterns: ["**/*.{js,css,html,svg,png,woff2}"],
        navigateFallback: "/index.html",
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/.*\/api\//,
            handler: "NetworkFirst",
            options: {
              cacheName: "api-cache",
              expiration: { maxEntries: 50, maxAgeSeconds: 60 },
            },
          },
        ],
      },
    }),
  ],
});
```

---

## Offline Behavior

| State           | Behavior                                                 |
| --------------- | -------------------------------------------------------- |
| **Online**      | Full functionality, SSE connected                        |
| **Offline**     | App shell loads from cache, shows "No connection" banner |
| **Back online** | SSE reconnects automatically, banner dismissed           |

The app is **not** designed for offline use — media playback requires the API. But the shell (HTML/CSS/JS) is cached so the app opens instantly and shows a clear connection status.

---

## Install Prompt

```typescript
// src/composables/useInstallPrompt.ts
export function useInstallPrompt() {
  const canInstall = ref(false);
  let deferredPrompt: BeforeInstallPromptEvent | null = null;

  window.addEventListener("beforeinstallprompt", (e) => {
    e.preventDefault();
    deferredPrompt = e;
    canInstall.value = true;
  });

  async function install() {
    if (!deferredPrompt) return;
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    deferredPrompt = null;
    canInstall.value = false;
    return outcome;
  }

  return { canInstall, install };
}
```

Show a subtle install banner at the bottom of the queue view (not a fullscreen modal):

```
┌─────────────────────────────┐
│ 📱 Install Media Platform   │
│ Add to home screen  [Install] [Dismiss] │
└─────────────────────────────┘
```

---

## Assets Needed

| Asset                  | Size    | Purpose                  |
| ---------------------- | ------- | ------------------------ |
| `pwa-192.png`          | 192×192 | Android home screen icon |
| `pwa-512.png`          | 512×512 | Android splash screen    |
| `apple-touch-icon.png` | 180×180 | iOS home screen icon     |
| `favicon.svg`          | any     | Browser tab icon         |

Design: Dark background (#0a0a0f), accent (#ff3366), play-button or music note icon.

---

## Tasks

- [ ] Configure VitePWA plugin in `vite.config.ts`
- [ ] Create PWA manifest with correct theme/background colors
- [ ] Create icon assets (192, 512, apple-touch-icon, favicon)
- [ ] Implement `useInstallPrompt` composable
- [ ] Add install banner component to queue view
- [ ] Configure workbox caching strategies
- [ ] Test install flow on Android Chrome
- [ ] Test "Add to Home Screen" on iOS Safari
- [ ] Verify offline shell loads correctly

---

## Acceptance Criteria

- [ ] Lighthouse PWA audit passes
- [ ] "Add to Home Screen" prompt appears on mobile
- [ ] App opens from home screen in standalone mode
- [ ] Offline shell loads from cache
- [ ] Correct splash screen and icons shown
- [ ] Auto-update when new version is deployed
