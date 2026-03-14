<script setup lang="ts">
import type { Ref } from "vue";
import { Activity, AlertTriangle, Clock, Zap } from "lucide-vue-next";
import { Card } from "@/shared/components/ui/card";
import { Skeleton } from "@/shared/components/ui/skeleton";
import { useGetAnalytics } from "@/generated/analytics/analytics";

const analyticsQuery = useGetAnalytics(undefined, {
  query: { refetchInterval: 15_000 },
});
const analytics = analyticsQuery.data as Ref<Record<string, number> | undefined>;
const isLoading = analyticsQuery.isLoading;

const cards = [
  { key: "totalCommands", label: "Total Commands", icon: Zap, suffix: "" },
  { key: "totalErrors", label: "Errors", icon: AlertTriangle, suffix: "" },
  { key: "avgLatencyMs", label: "Avg Latency", icon: Clock, suffix: "ms" },
  { key: "totalPlaybackMinutes", label: "Playback", icon: Activity, suffix: "min" },
] as const;

function getValue(key: string): string {
  if (!analytics.value) return "—";
  const val = analytics.value[key];
  if (val === undefined || val === null) return "—";
  return typeof val === "number" ? val.toLocaleString() : String(val);
}
</script>

<template>
  <div class="grid grid-cols-2 gap-3 md:grid-cols-4">
    <template v-if="isLoading">
      <Skeleton v-for="i in 4" :key="i" class="h-24 rounded-xl" />
    </template>
    <Card
      v-else
      v-for="card in cards"
      :key="card.key"
      class="flex flex-col gap-1 p-4"
    >
      <div class="flex items-center gap-2 text-xs text-muted-foreground">
        <component :is="card.icon" class="h-3.5 w-3.5" />
        {{ card.label }}
      </div>
      <p class="text-2xl font-bold tabular-nums">
        {{ getValue(card.key) }}<span v-if="card.suffix" class="text-sm font-normal text-muted-foreground">{{ card.suffix }}</span>
      </p>
    </Card>
  </div>
</template>
