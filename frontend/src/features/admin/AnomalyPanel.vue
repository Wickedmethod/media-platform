<script setup lang="ts">
import type { Ref } from "vue";
import { ShieldCheck, AlertTriangle } from "lucide-vue-next";
import { Card } from "@/shared/components/ui/card";
import { Badge } from "@/shared/components/ui/badge";
import { useGetAnomalies } from "@/generated/admin/admin";

const anomalyQuery = useGetAnomalies({
  query: { refetchInterval: 30_000 },
});
const anomalies = anomalyQuery.data as Ref<Record<string, unknown> | undefined>;
const isLoading = anomalyQuery.isLoading;

interface AnomalyReport {
  detected: boolean;
  alerts?: { type: string; message: string; timestamp: string }[];
}

function getReport(): AnomalyReport {
  if (!anomalies.value) return { detected: false };
  return {
    detected: !!anomalies.value.detected || (Array.isArray(anomalies.value.alerts) && anomalies.value.alerts.length > 0),
    alerts: (anomalies.value.alerts as AnomalyReport["alerts"]) ?? [],
  };
}
</script>

<template>
  <Card class="p-4">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <AlertTriangle
          v-if="getReport().detected"
          class="h-4 w-4 text-yellow-500"
        />
        <ShieldCheck v-else class="h-4 w-4 text-emerald-500" />
        <h3 class="text-sm font-semibold">Anomaly Detection</h3>
      </div>
      <Badge :variant="getReport().detected ? 'destructive' : 'secondary'">
        {{ getReport().detected ? "ALERT" : "Clean" }}
      </Badge>
    </div>

    <div v-if="isLoading" class="mt-2">
      <div class="h-4 animate-pulse rounded bg-muted" />
    </div>

    <div
      v-else-if="getReport().detected && getReport().alerts?.length"
      class="mt-3 space-y-1"
    >
      <div
        v-for="(alert, idx) in getReport().alerts"
        :key="idx"
        class="rounded bg-destructive/10 px-2 py-1 text-xs text-destructive"
      >
        {{ alert.message }}
      </div>
    </div>

    <p v-else class="mt-2 text-xs text-muted-foreground">
      No anomalies detected
    </p>
  </Card>
</template>
