<script setup lang="ts">
import { useFeatureFlags } from "@/composables/useFeatureFlags";
import { Switch } from "@/shared/components/ui/switch";
import { Button } from "@/shared/components/ui/button";
import type { FlagKey } from "@/config/flags.config";

const { isEnabled, setOverride, clearAllOverrides, flags } = useFeatureFlags();
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-6 p-6">
    <div class="flex items-center justify-between">
      <h2 class="text-lg font-semibold">Feature Flags</h2>
      <Button variant="outline" size="sm" @click="clearAllOverrides"
        >Reset All</Button
      >
    </div>

    <div class="divide-y rounded-lg border">
      <div
        v-for="(config, key) in flags"
        :key="key"
        class="flex items-center justify-between p-4"
      >
        <div>
          <p class="font-medium">{{ key }}</p>
          <p class="text-sm text-muted-foreground">{{ config.description }}</p>
          <span
            v-if="!config.overridable"
            class="text-xs text-muted-foreground/60"
          >
            Not overridable
          </span>
        </div>
        <Switch
          :checked="isEnabled(key as FlagKey)"
          :disabled="!config.overridable"
          @update:checked="(val: boolean) => setOverride(key as FlagKey, val)"
        />
      </div>
    </div>
  </div>
</template>
