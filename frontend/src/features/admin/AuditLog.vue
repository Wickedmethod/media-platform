<script setup lang="ts">
import { ScrollText } from "lucide-vue-next";
import { Card } from "@/shared/components/ui/card";
import { Badge } from "@/shared/components/ui/badge";
import { useGetAuditLog } from "@/generated/admin/admin";

const { data: auditLog, isLoading } = useGetAuditLog({ count: 20 }, {
  query: { refetchInterval: 15_000 },
});

interface AuditEntry {
  timestamp: string;
  action: string;
  path?: string;
  statusCode?: number;
  detail?: string;
}

function formatTime(ts: string): string {
  try {
    return new Date(ts).toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return ts;
  }
}

function statusVariant(code?: number): "default" | "secondary" | "destructive" {
  if (!code) return "secondary";
  if (code >= 400) return "destructive";
  return "default";
}
</script>

<template>
  <Card class="p-4">
    <div class="mb-3 flex items-center gap-2">
      <ScrollText class="h-4 w-4 text-muted-foreground" />
      <h3 class="text-sm font-semibold">Recent Audit Log</h3>
    </div>

    <div v-if="isLoading" class="space-y-2">
      <div v-for="i in 5" :key="i" class="h-6 animate-pulse rounded bg-muted" />
    </div>

    <div
      v-else-if="auditLog && (auditLog as AuditEntry[]).length > 0"
      class="max-h-64 space-y-1 overflow-y-auto"
    >
      <div
        v-for="(entry, idx) in (auditLog as AuditEntry[])"
        :key="idx"
        class="flex items-center gap-2 rounded px-2 py-1 text-xs hover:bg-muted"
      >
        <span class="w-12 flex-shrink-0 text-muted-foreground tabular-nums">
          {{ formatTime(entry.timestamp) }}
        </span>
        <span class="truncate font-mono">
          {{ entry.action ?? entry.path ?? "—" }}
        </span>
        <Badge
          v-if="entry.statusCode"
          :variant="statusVariant(entry.statusCode)"
          class="ml-auto flex-shrink-0 text-[10px]"
        >
          {{ entry.statusCode }}
        </Badge>
      </div>
    </div>

    <p v-else class="text-center text-sm text-muted-foreground py-4">
      No audit entries
    </p>
  </Card>
</template>
