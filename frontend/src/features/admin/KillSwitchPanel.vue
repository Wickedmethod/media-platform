<script setup lang="ts">
import { ref, type Ref } from "vue";
import { ShieldAlert, ShieldCheck, Loader2 } from "lucide-vue-next";
import { useQueryClient } from "@tanstack/vue-query";
import { Card } from "@/shared/components/ui/card";
import { Button } from "@/shared/components/ui/button";
import { Badge } from "@/shared/components/ui/badge";
import { Input } from "@/shared/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/components/ui/dialog";
import {
  useGetKillSwitchStatus,
  getGetKillSwitchStatusQueryKey,
  useActivateKillSwitch,
  useDeactivateKillSwitch,
} from "@/generated/admin/admin";
import { useToast } from "@/composables/useToast";

const queryClient = useQueryClient();
const toast = useToast();
const showDialog = ref(false);
const reason = ref("");

const killSwitchQuery = useGetKillSwitchStatus();
const status = killSwitchQuery.data as Ref<Record<string, unknown> | undefined>;

const activateMutation = useActivateKillSwitch({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: getGetKillSwitchStatusQueryKey(),
      });
      toast.warning("Kill switch activated");
      showDialog.value = false;
      reason.value = "";
    },
    onError: () => toast.error("Failed to activate kill switch"),
  },
});

const deactivateMutation = useDeactivateKillSwitch({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: getGetKillSwitchStatusQueryKey(),
      });
      toast.success("Kill switch deactivated");
    },
    onError: () => toast.error("Failed to deactivate kill switch"),
  },
});

const isActive = () => {
  if (!status.value) return false;
  return status.value.active === true;
};

function handleActivate() {
  if (!reason.value.trim()) return;
  activateMutation.mutate({ data: { reason: reason.value.trim() } });
}

function handleDeactivate() {
  deactivateMutation.mutate();
}
</script>

<template>
  <Card class="p-4">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <ShieldAlert v-if="isActive()" class="h-5 w-5 text-destructive" />
        <ShieldCheck v-else class="h-5 w-5 text-emerald-500" />
        <div>
          <h3 class="text-sm font-semibold">Kill Switch</h3>
          <p class="text-xs text-muted-foreground">
            {{ isActive() ? "All writes blocked" : "Operational" }}
          </p>
        </div>
      </div>
      <div class="flex items-center gap-2">
        <Badge :variant="isActive() ? 'destructive' : 'secondary'">
          {{ isActive() ? "ACTIVE" : "OFF" }}
        </Badge>
        <Button
          v-if="isActive()"
          variant="outline"
          size="sm"
          :disabled="deactivateMutation.isPending.value"
          @click="handleDeactivate"
        >
          <Loader2
            v-if="deactivateMutation.isPending.value"
            class="mr-1 h-4 w-4 animate-spin"
          />
          Deactivate
        </Button>
        <Button
          v-else
          variant="destructive"
          size="sm"
          @click="showDialog = true"
        >
          Activate
        </Button>
      </div>
    </div>

    <Dialog v-model:open="showDialog">
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Activate Kill Switch</DialogTitle>
          <DialogDescription>
            This will block all write operations. Only admins can deactivate.
          </DialogDescription>
        </DialogHeader>
        <Input
          v-model="reason"
          placeholder="Reason for activation..."
          @keydown.enter="handleActivate"
        />
        <DialogFooter>
          <Button variant="outline" @click="showDialog = false">Cancel</Button>
          <Button
            variant="destructive"
            :disabled="
              !reason.trim() || activateMutation.isPending.value
            "
            @click="handleActivate"
          >
            <Loader2
              v-if="activateMutation.isPending.value"
              class="mr-1 h-4 w-4 animate-spin"
            />
            Confirm Activate
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  </Card>
</template>
