# MEDIA-714: Loading States & Skeleton Screens

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-700 (project setup)

---

## Summary

Create reusable loading state components — skeleton screens, spinners, and empty states — used across all views. Data-fetching components never show blank screens; they always show either content, a skeleton, or an empty state.

**Absorbs:** MEDIA-750 (Queue Empty State UI) — empty states for queue and other views are fully covered here.

---

## Component Library

### 1. Skeleton Components

```vue
<!-- src/shared/components/ui/Skeleton.vue -->
<!-- Already provided by shadcn-vue: <Skeleton class="h-4 w-full" /> -->
```

Compose into domain-specific skeletons:

```vue
<!-- src/shared/components/QueueItemSkeleton.vue -->
<template>
  <div class="flex items-center gap-3 p-3 rounded-lg">
    <Skeleton class="h-12 w-12 rounded" />
    <!-- thumbnail -->
    <div class="flex-1 space-y-2">
      <Skeleton class="h-4 w-3/4" />
      <!-- title -->
      <Skeleton class="h-3 w-1/3" />
      <!-- added by -->
    </div>
    <Skeleton class="h-8 w-8 rounded-full" />
    <!-- action button -->
  </div>
</template>
```

```vue
<!-- src/shared/components/NowPlayingSkeleton.vue -->
<template>
  <div class="flex items-center gap-3 p-4">
    <Skeleton class="h-10 w-10 rounded-full" />
    <!-- play/pause -->
    <div class="flex-1 space-y-2">
      <Skeleton class="h-4 w-2/3" />
      <!-- song title -->
      <Skeleton class="h-1 w-full rounded" />
      <!-- progress bar -->
    </div>
    <Skeleton class="h-3 w-12" />
    <!-- time -->
  </div>
</template>
```

### 2. Empty States

```vue
<!-- src/shared/components/EmptyState.vue -->
<template>
  <div class="flex flex-col items-center justify-center py-16 text-center">
    <component :is="icon" class="h-12 w-12 text-muted-foreground mb-4" />
    <h3 class="text-lg font-medium">{{ title }}</h3>
    <p class="text-sm text-muted-foreground mt-1 max-w-xs">{{ description }}</p>
    <slot name="action" />
  </div>
</template>
```

Usage:

```vue
<EmptyState
  :icon="ListMusic"
  title="Queue is empty"
  description="Search for a song and add it to the queue"
>
  <template #action>
    <Button class="mt-4" @click="router.push('/search')">
      <Search class="h-4 w-4 mr-2" /> Search Music
    </Button>
  </template>
</EmptyState>
```

### 3. Inline Spinner

```vue
<!-- src/shared/components/ui/Spinner.vue -->
<template>
  <div class="flex items-center justify-center" :class="sizeClasses">
    <Loader2 :class="['animate-spin text-muted-foreground', iconSize]" />
  </div>
</template>
```

---

## Query State Pattern

Every data-fetching component follows this pattern:

```vue
<template>
  <!-- Loading -->
  <QueueItemSkeleton v-if="isLoading" v-for="i in 5" :key="i" />

  <!-- Error -->
  <ErrorState v-else-if="error" :error="error" @retry="refetch" />

  <!-- Empty -->
  <EmptyState
    v-else-if="data?.length === 0"
    :icon="ListMusic"
    title="Queue is empty"
    description="Add a song to get started"
  />

  <!-- Content -->
  <QueueItem v-else v-for="item in data" :key="item.id" :item="item" />
</template>
```

---

## Standardized Empty States

| View                | Icon      | Title            | Description                          | Action       |
| ------------------- | --------- | ---------------- | ------------------------------------ | ------------ |
| Queue (empty)       | ListMusic | Queue is empty   | Search for a song to get started     | → Search     |
| Search (no results) | SearchX   | No results       | Try a different search term          | —            |
| Search (initial)    | Search    | Search YouTube   | Type to search for music             | —            |
| Admin audit (empty) | FileText  | No audit entries | System actions will appear here      | —            |
| Policies (empty)    | Shield    | No policies      | Add a playback policy to get started | + Add Policy |

---

## Tasks

- [ ] Install shadcn-vue `Skeleton` component
- [ ] Create `QueueItemSkeleton.vue`
- [ ] Create `NowPlayingSkeleton.vue`
- [ ] Create `DashboardCardSkeleton.vue`
- [ ] Create `EmptyState.vue` (generic, with icon/title/description/action slot)
- [ ] Create `ErrorState.vue` (with retry button)
- [ ] Create `Spinner.vue` (inline loading indicator)
- [ ] Apply skeleton/empty pattern to QueueView
- [ ] Apply skeleton/empty pattern to SearchView
- [ ] Apply skeleton/empty pattern to AdminDashboard
- [ ] Ensure all TanStack Query usages handle `isLoading` / `error` / empty

---

## Acceptance Criteria

- [ ] No view ever shows a blank white screen while loading
- [ ] Skeleton shapes match the layout of actual content
- [ ] Empty states have clear messaging and an action button where appropriate
- [ ] Error states show a retry button that refetches data
- [ ] Skeletons animate (pulse) to indicate loading
- [ ] Loading → Content transition is smooth (no layout shift)
