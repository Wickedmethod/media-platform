<script setup lang="ts">
import { computed } from "vue";
import { Music, User } from "lucide-vue-next";
import { usePlayerStore } from "@/stores/player";
import { Card, CardContent } from "@/shared/components/ui/card";

const player = usePlayerStore();

const formattedPosition = computed(() => formatTime(player.position));
const formattedDuration = computed(() => formatTime(player.duration));

const stateBadge = computed(() => {
  const map: Record<string, { label: string; class: string }> = {
    Playing: { label: "Playing", class: "bg-emerald-500/10 text-emerald-500" },
    Paused: { label: "Paused", class: "bg-amber-500/10 text-amber-500" },
    Stopped: { label: "Stopped", class: "bg-muted text-muted-foreground" },
    Idle: { label: "Idle", class: "bg-muted text-muted-foreground" },
  };
  return map[player.playerState] ?? map.Idle;
});

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}
</script>

<template>
  <Card>
    <CardContent class="p-4">
      <div v-if="player.currentItem" class="space-y-3">
        <div class="flex items-start gap-3">
          <!-- Thumbnail or music icon -->
          <div
            class="flex h-14 w-14 shrink-0 items-center justify-center rounded-md bg-muted"
          >
            <img
              v-if="player.currentItem.thumbnailUrl"
              :src="player.currentItem.thumbnailUrl"
              :alt="player.currentItem.title"
              class="h-full w-full rounded-md object-cover"
            />
            <Music v-else class="h-6 w-6 text-muted-foreground" />
          </div>

          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <span
                :class="[
                  'inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium',
                  stateBadge!.class,
                ]"
              >
                {{ stateBadge!.label }}
              </span>
            </div>
            <p class="mt-1 truncate text-sm font-medium">
              {{ player.currentItem.title }}
            </p>
            <div
              v-if="player.currentItem.addedByName"
              class="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground"
            >
              <User class="h-3 w-3" />
              {{ player.currentItem.addedByName }}
            </div>
          </div>
        </div>

        <!-- Progress -->
        <div class="space-y-1">
          <div class="h-1 w-full overflow-hidden rounded-full bg-muted">
            <div
              class="h-full rounded-full bg-primary transition-[width] duration-1000 ease-linear"
              :style="{ width: `${player.progress}%` }"
            />
          </div>
          <div class="flex justify-between text-[10px] tabular-nums text-muted-foreground">
            <span>{{ formattedPosition }}</span>
            <span>{{ formattedDuration }}</span>
          </div>
        </div>

        <!-- Error -->
        <p
          v-if="player.lastError"
          class="text-xs text-destructive"
        >
          {{ player.lastError }}
        </p>
      </div>

      <!-- Idle state -->
      <div v-else class="flex items-center gap-3 py-2">
        <Music class="h-5 w-5 text-muted-foreground" />
        <p class="text-sm text-muted-foreground">Nothing playing</p>
      </div>
    </CardContent>
  </Card>
</template>
