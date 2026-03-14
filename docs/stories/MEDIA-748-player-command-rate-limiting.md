# MEDIA-748: Player Command Rate Limiting & Debounce

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-704 (player store), MEDIA-702 (queue view)

---

## Summary

Prevent rapid-fire player commands from the SPA. When users hammer play/pause/skip buttons, debounce at the UI level and deduplicate at the API level. This complements the existing server-side rate limiting (MEDIA-604) with client-side UX protection.

**Related:** MEDIA-604 (server rate limits — already done, prevents abuse). This story is the **frontend UX** counterpart.

---

## Client-Side Debounce

### usePlayerCommands Composable

```typescript
// src/composables/usePlayerCommands.ts
import { ref } from "vue";
import { useMutation } from "@tanstack/vue-query";
import { playerApi } from "@/api/generated";

export function usePlayerCommands() {
  const pendingCommand = ref<string | null>(null);

  const playMutation = useMutation({
    mutationFn: () => playerApi.play(),
    onMutate: () => {
      pendingCommand.value = "play";
    },
    onSettled: () => {
      pendingCommand.value = null;
    },
  });

  const pauseMutation = useMutation({
    mutationFn: () => playerApi.pause(),
    onMutate: () => {
      pendingCommand.value = "pause";
    },
    onSettled: () => {
      pendingCommand.value = null;
    },
  });

  const skipMutation = useMutation({
    mutationFn: () => playerApi.skip(),
    onMutate: () => {
      pendingCommand.value = "skip";
    },
    onSettled: () => {
      pendingCommand.value = null;
    },
  });

  return {
    play: () => {
      if (!pendingCommand.value) playMutation.mutate();
    },
    pause: () => {
      if (!pendingCommand.value) pauseMutation.mutate();
    },
    skip: () => {
      if (!pendingCommand.value) skipMutation.mutate();
    },
    pendingCommand,
    isDisabled: computed(() => !!pendingCommand.value),
  };
}
```

### Button UX

```vue
<!-- Player controls in queue view -->
<Button
  @click="commands.play()"
  :disabled="commands.isDisabled.value"
  :class="{ 'opacity-50': commands.isDisabled.value }"
>
  <Loader2 v-if="commands.pendingCommand.value === 'play'" class="animate-spin" />
  <Play v-else />
</Button>
```

Behavior:

- Button shows spinner while command is in-flight
- All other buttons disabled until response arrives
- Prevents double-tap and rapid toggling

---

## Server-Side Command Deduplication

### Idempotent Command Guard

```csharp
// Middleware or service-level check
public class CommandDeduplicationFilter(IConnectionMultiplexer redis) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var path = context.HttpContext.Request.Path.Value;
        var key = $"cmd:dedup:{path}";

        // Reject if same command was sent within 500ms
        var wasSet = await redis.GetDatabase().StringSetAsync(
            key, "1", TimeSpan.FromMilliseconds(500), When.NotExists);

        if (!wasSet)
            return Results.Ok(new { deduplicated = true });

        return await next(context);
    }
}
```

Apply to player command endpoints:

```csharp
app.MapPost("/player/play", ...).AddEndpointFilter<CommandDeduplicationFilter>();
app.MapPost("/player/pause", ...).AddEndpointFilter<CommandDeduplicationFilter>();
app.MapPost("/player/skip", ...).AddEndpointFilter<CommandDeduplicationFilter>();
```

---

## Debounce Timing

| Layer             | Mechanism                | Window                 |
| ----------------- | ------------------------ | ---------------------- |
| Frontend (SPA)    | `pendingCommand` gate    | Until response arrives |
| Frontend (TV/CEC) | CEC key repeat filter    | 300ms (MEDIA-721)      |
| Backend           | Redis `SETNX` dedup      | 500ms per endpoint     |
| Backend           | Rate limiter (MEDIA-604) | 30 req/min per IP      |

---

## Tasks

- [ ] Create `usePlayerCommands` composable with mutation-based gating
- [ ] Add spinner + disabled state to player control buttons
- [ ] Create `CommandDeduplicationFilter` endpoint filter
- [ ] Apply filter to `/player/play`, `/player/pause`, `/player/skip`
- [ ] Unit tests for composable (pending state, disable logic)
- [ ] Unit tests for dedup filter (allow first, reject duplicate, allow after TTL)

---

## Acceptance Criteria

- [ ] Rapid clicks on play/pause only send one request
- [ ] Button shows spinner while command is in-flight
- [ ] All player buttons disabled during pending command
- [ ] Server rejects duplicate commands within 500ms window
- [ ] Normal usage (>500ms between clicks) works without issue
