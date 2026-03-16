import { apiFetch } from "@/composables/useApi";

export interface SessionResponse {
  sessionId: string;
  userId: string | null;
  deviceId: string | null;
  type: string;
  createdAt: string;
  lastActivityAt: string;
}

export interface SessionSnapshotResponse {
  session: SessionResponse;
  queue: SessionQueueItem[];
  playback: SessionPlaybackState;
}

export interface SessionQueueItem {
  id: string;
  url: string;
  title: string;
  status: string;
  addedAt: string;
  startAtSeconds: number;
  addedByUserId?: string | null;
  addedByName?: string | null;
  channel?: string | null;
  durationSeconds?: number | null;
  thumbnailUrl?: string | null;
}

export interface SessionPlaybackState {
  state: string;
  currentItem: SessionQueueItem | null;
  startedAt: string | null;
  positionSeconds: number;
  retryCount: number;
  lastError: string | null;
}

const BASE = "/v1/sessions";

export function createPersonalSession(deviceId: string) {
  return apiFetch<SessionResponse>(`${BASE}/personal`, {
    method: "POST",
    body: JSON.stringify({ deviceId }),
  });
}

export function getMySession() {
  return apiFetch<SessionSnapshotResponse>(`${BASE}/mine`);
}

export function addToSessionQueue(
  sessionId: string,
  url: string,
  title: string,
  startAtSeconds = 0,
) {
  return apiFetch<SessionQueueItem>(
    `${BASE}/${encodeURIComponent(sessionId)}/queue/add`,
    {
      method: "POST",
      body: JSON.stringify({ url, title, startAtSeconds }),
    },
  );
}

export function sessionPlayerCommand(sessionId: string, action: string) {
  return apiFetch<SessionPlaybackState>(
    `${BASE}/${encodeURIComponent(sessionId)}/player/${action}`,
    { method: "POST" },
  );
}

export function endSession(sessionId: string) {
  return apiFetch<void>(`${BASE}/${encodeURIComponent(sessionId)}`, {
    method: "DELETE",
  });
}

export function getSessionEventsUrl(sessionId: string, token?: string) {
  const base = `/api/v1/sessions/${encodeURIComponent(sessionId)}/events`;
  return token ? `${base}?token=${encodeURIComponent(token)}` : base;
}
