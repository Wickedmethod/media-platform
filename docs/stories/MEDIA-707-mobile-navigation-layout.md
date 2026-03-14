# MEDIA-707: Mobile Navigation & App Layout

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** High  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup), MEDIA-701 (auth)

---

## Summary

Create the responsive app shell with a bottom tab bar (mobile) and sidebar (desktop). Navigation items are role-based: users see Queue + Search, admins also see Admin dashboard. A persistent "Now Playing" mini-bar sits above the navigation.

---

## Layout Architecture

```
┌──────────────────────────────────┐
│ Header (optional, desktop only)  │
├──────────────────────────────────┤
│                                  │
│         <RouterView />           │
│                                  │
│                                  │
├──────────────────────────────────┤
│ ♫ Now Playing ▶ Title   0:42    │  ← NowPlayingBar (persistent)
├──────────────────────────────────┤
│ 🎵 Queue  │ 🔍 Search │ ⚙ Admin │  ← BottomTabBar (mobile)
└──────────────────────────────────┘
```

### Desktop (≥768px)

```
┌─────────┬───────────────────────────┐
│ Sidebar │                           │
│         │                           │
│ 🎵 Queue│     <RouterView />        │
│ 🔍 Search                           │
│ ⚙ Admin │                           │
│         │                           │
│         ├───────────────────────────┤
│ User    │ ♫ Now Playing ▶ 0:42     │
└─────────┴───────────────────────────┘
```

---

## Routes

| Path | Component | Role | Tab |
|------|-----------|------|-----|
| `/` | Redirect to `/queue` | — | — |
| `/queue` | QueueView | All | 🎵 Queue |
| `/search` | SearchView | All | 🔍 Search |
| `/admin` | AdminDashboard | `media-admin` | ⚙ Admin |
| `/login` | LoginRedirect | — | — |

---

## Bottom Tab Bar Component

```vue
<!-- src/shared/components/BottomTabBar.vue -->
<template>
  <nav class="fixed bottom-0 inset-x-0 bg-card border-t md:hidden">
    <div class="flex justify-around">
      <RouterLink
        v-for="tab in visibleTabs"
        :key="tab.path"
        :to="tab.path"
        class="flex flex-col items-center py-2 px-3"
        active-class="text-primary"
      >
        <component :is="tab.icon" class="h-5 w-5" />
        <span class="text-xs mt-1">{{ tab.label }}</span>
      </RouterLink>
    </div>
  </nav>
</template>
```

### Tabs Configuration

```typescript
const tabs = [
  { path: '/queue', label: 'Queue', icon: ListMusic, roles: ['media-user', 'media-admin'] },
  { path: '/search', label: 'Search', icon: Search, roles: ['media-user', 'media-admin'] },
  { path: '/admin', label: 'Admin', icon: Settings, roles: ['media-admin'] },
]

const visibleTabs = computed(() =>
  tabs.filter(tab => tab.roles.some(role => authStore.hasRole(role)))
)
```

---

## Now Playing Mini-Bar

A persistent bar above the bottom navigation showing:

```
┌─────────────────────────────────┐
│ 🎵  Song Title — Artist   ▶ 2:34 │
│ ████████████░░░░░░░░░░░░        │  ← progress bar
└─────────────────────────────────┘
```

- Shows `currentItem.title` from `usePlayerStore`
- Progress bar (thin, accent color) from SSE position updates
- Tapping opens a "full" now-playing sheet (future story)
- Hidden when `playerState === 'Idle'`

---

## App Shell Component

```vue
<!-- src/App.vue -->
<template>
  <div class="min-h-screen bg-background text-foreground">
    <!-- Desktop sidebar -->
    <Sidebar v-if="isDesktop" />

    <main :class="{ 'md:ml-64': isDesktop, 'pb-28': isMobile }">
      <RouterView />
    </main>

    <!-- Mobile: Now Playing + Bottom Nav -->
    <div v-if="isMobile" class="fixed bottom-0 inset-x-0 z-50">
      <NowPlayingBar v-if="playerStore.currentItem" />
      <BottomTabBar />
    </div>

    <!-- Desktop: Now Playing bar at bottom of main area -->
    <NowPlayingBar
      v-if="isDesktop && playerStore.currentItem"
      class="fixed bottom-0 right-0 left-64"
    />

    <!-- Connection status indicator -->
    <ConnectionStatus />
  </div>
</template>
```

---

## Design Tokens

Following the existing index.html dark theme:

| Token | Value | Usage |
|-------|-------|-------|
| `--background` | `#0a0a0f` | App background |
| `--foreground` | `#e0e0e0` | Primary text |
| `--card` | `#12121a` | Card/surface |
| `--primary` | `#ff3366` | Accent, active tab |
| `--muted` | `#6a6a7a` | Inactive tab icons |
| `--border` | `#1a1a2e` | Dividers |

These map to shadcn-vue CSS variables in `tailwind.config.ts`.

---

## Tasks

- [ ] Create `AppLayout.vue` with responsive shell
- [ ] Create `BottomTabBar.vue` with role-based tabs
- [ ] Create `Sidebar.vue` for desktop layout
- [ ] Create `NowPlayingBar.vue` with player store binding
- [ ] Create `ConnectionStatus.vue` indicator
- [ ] Configure CSS variables matching dark theme
- [ ] Set up Vue Router with routes and guards
- [ ] Add route transitions (slide for mobile, fade for desktop)
- [ ] Test responsive breakpoints (mobile → tablet → desktop)

---

## Acceptance Criteria

- [ ] Bottom tab bar shown on mobile, sidebar on desktop
- [ ] Admin tab only visible to `media-admin` role
- [ ] Now Playing bar shows current track with progress
- [ ] Now Playing bar hidden when nothing playing
- [ ] Route transitions are smooth
- [ ] Active tab/route highlighted with accent color
- [ ] Safe area insets handled (notch phones)
- [ ] Keyboard doesn't push bottom bar up (search input)
