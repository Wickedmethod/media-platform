<script setup lang="ts">
import { ref } from "vue";
import { Plus, Trash2, Loader2 } from "lucide-vue-next";
import { useQueryClient } from "@tanstack/vue-query";
import { Card } from "@/shared/components/ui/card";
import { Button } from "@/shared/components/ui/button";
import { Badge } from "@/shared/components/ui/badge";
import { Input } from "@/shared/components/ui/input";
import { Switch } from "@/shared/components/ui/switch";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/components/ui/select";
import {
  useGetPolicies,
  getGetPoliciesQueryKey,
  useAddPolicy,
  useRemovePolicy,
  useTogglePolicy,
} from "@/generated/policies/policies";
import { useToast } from "@/composables/useToast";
import type { PolicySnapshot } from "@/generated/models";

const queryClient = useQueryClient();
const toast = useToast();
const showAddDialog = ref(false);
const newPolicyName = ref("");
const newPolicyType = ref("BlockedChannel");
const newPolicyValue = ref("");
const removingId = ref<string | null>(null);

const POLICY_TYPES = [
  "BlockedChannel",
  "BlockedUrlPattern",
  "TimeWindow",
  "MaxQueueSize",
  "MaxDuration",
];

const { data: policies, isLoading } = useGetPolicies();

const addMutation = useAddPolicy({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPoliciesQueryKey() });
      toast.success("Policy added");
      showAddDialog.value = false;
      newPolicyName.value = "";
      newPolicyValue.value = "";
    },
    onError: () => toast.error("Failed to add policy"),
  },
});

const removeMutation = useRemovePolicy({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPoliciesQueryKey() });
      toast.success("Policy removed");
      removingId.value = null;
    },
    onError: () => {
      toast.error("Failed to remove policy");
      removingId.value = null;
    },
  },
});

const toggleMutation = useTogglePolicy({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetPoliciesQueryKey() });
    },
    onError: () => toast.error("Failed to toggle policy"),
  },
});

function handleAdd() {
  if (!newPolicyName.value.trim() || !newPolicyValue.value.trim()) return;
  addMutation.mutate({
    data: {
      name: newPolicyName.value.trim(),
      type: newPolicyType.value,
      value: newPolicyValue.value.trim(),
      enabled: true,
    },
  });
}

function handleRemove(id: string) {
  removingId.value = id;
  removeMutation.mutate({ id });
}

function handleToggle(policy: PolicySnapshot) {
  toggleMutation.mutate({ id: policy.id, data: { enabled: !policy.enabled } });
}
</script>

<template>
  <Card class="p-4">
    <div class="mb-3 flex items-center justify-between">
      <h3 class="text-sm font-semibold">
        Policies
        <Badge v-if="policies" variant="secondary" class="ml-1">
          {{ (policies as PolicySnapshot[]).filter((p) => p.enabled).length }}
          active
        </Badge>
      </h3>
      <Button variant="outline" size="sm" @click="showAddDialog = true">
        <Plus class="mr-1 h-4 w-4" />
        Add
      </Button>
    </div>

    <div v-if="isLoading" class="space-y-2">
      <div v-for="i in 3" :key="i" class="h-10 animate-pulse rounded bg-muted" />
    </div>

    <div
      v-else-if="policies && (policies as PolicySnapshot[]).length > 0"
      class="space-y-2"
    >
      <div
        v-for="policy in (policies as PolicySnapshot[])"
        :key="policy.id"
        class="flex items-center justify-between rounded-lg border p-3"
      >
        <div class="flex items-center gap-3">
          <Switch
            :checked="policy.enabled"
            @update:checked="handleToggle(policy)"
          />
          <div>
            <p class="text-sm font-medium">{{ policy.name }}</p>
            <p class="text-xs text-muted-foreground">{{ policy.type }}</p>
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          :disabled="removingId === policy.id"
          @click="handleRemove(policy.id)"
        >
          <Loader2
            v-if="removingId === policy.id"
            class="h-4 w-4 animate-spin"
          />
          <Trash2 v-else class="h-4 w-4 text-muted-foreground" />
        </Button>
      </div>
    </div>

    <p v-else class="text-center text-sm text-muted-foreground py-4">
      No policies configured
    </p>

    <Dialog v-model:open="showAddDialog">
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add Policy</DialogTitle>
        </DialogHeader>
        <div class="space-y-3">
          <Input
            v-model="newPolicyName"
            placeholder="Policy name"
          />
          <Select v-model="newPolicyType">
            <SelectTrigger>
              <SelectValue placeholder="Policy type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="t in POLICY_TYPES" :key="t" :value="t">
                {{ t }}
              </SelectItem>
            </SelectContent>
          </Select>
          <Input
            v-model="newPolicyValue"
            placeholder="Value (e.g. channel name, URL pattern)"
          />
        </div>
        <DialogFooter>
          <Button variant="outline" @click="showAddDialog = false">
            Cancel
          </Button>
          <Button
            :disabled="
              !newPolicyName.trim() ||
              !newPolicyValue.trim() ||
              addMutation.isPending.value
            "
            @click="handleAdd"
          >
            <Loader2
              v-if="addMutation.isPending.value"
              class="mr-1 h-4 w-4 animate-spin"
            />
            Add Policy
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  </Card>
</template>
