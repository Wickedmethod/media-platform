<script setup lang="ts">
import { useToast, type Toast } from "@/composables/useToast";
import {
  CheckCircle,
  AlertCircle,
  AlertTriangle,
  Info,
  X,
} from "lucide-vue-next";
import { type Component } from "vue";

const { toasts, dismiss } = useToast();

const typeConfig: Record<
  Toast["type"],
  { bg: string; border: string; icon: Component }
> = {
  success: {
    bg: "bg-emerald-500/10",
    border: "border-emerald-500/20",
    icon: CheckCircle,
  },
  error: {
    bg: "bg-red-500/10",
    border: "border-red-500/20",
    icon: AlertCircle,
  },
  warning: {
    bg: "bg-amber-500/10",
    border: "border-amber-500/20",
    icon: AlertTriangle,
  },
  info: { bg: "bg-blue-500/10", border: "border-blue-500/20", icon: Info },
};
</script>

<template>
  <Teleport to="body">
    <div class="fixed top-4 right-4 z-[100] flex flex-col gap-2 max-w-sm">
      <TransitionGroup
        enter-active-class="transition-all duration-300 ease-out"
        enter-from-class="opacity-0 translate-x-4"
        enter-to-class="opacity-100 translate-x-0"
        leave-active-class="transition-all duration-200 ease-in"
        leave-from-class="opacity-100 translate-x-0"
        leave-to-class="opacity-0 translate-x-4"
      >
        <div
          v-for="toast in toasts"
          :key="toast.id"
          :class="[
            'rounded-lg border px-4 py-3 shadow-lg backdrop-blur-sm',
            typeConfig[toast.type].bg,
            typeConfig[toast.type].border,
          ]"
        >
          <div class="flex items-start gap-3">
            <component
              :is="typeConfig[toast.type].icon"
              class="h-5 w-5 shrink-0 mt-0.5"
            />
            <div class="flex-1 min-w-0">
              <p class="font-medium text-sm">{{ toast.title }}</p>
              <p v-if="toast.message" class="text-xs mt-1 opacity-80">
                {{ toast.message }}
              </p>
            </div>
            <button
              @click="dismiss(toast.id)"
              class="shrink-0 opacity-50 hover:opacity-100"
            >
              <X class="h-4 w-4" />
            </button>
          </div>
          <button
            v-if="toast.action"
            @click="toast.action.onClick"
            class="mt-2 text-xs font-medium underline"
          >
            {{ toast.action.label }}
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
