<script setup lang="ts">
import { Shield } from "lucide-vue-next";
import StatusCards from "./StatusCards.vue";
import KillSwitchPanel from "./KillSwitchPanel.vue";
import PolicyManager from "./PolicyManager.vue";
import AuditLog from "./AuditLog.vue";
import AnomalyPanel from "./AnomalyPanel.vue";
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <div class="flex items-center gap-2">
      <Shield class="h-5 w-5" />
      <h1 class="text-lg font-bold">Admin Dashboard</h1>
    </div>

    <StatusCards />
    <KillSwitchPanel />
    <PolicyManager />
    <AuditLog />
    <AnomalyPanel />
  </div>
</template>
