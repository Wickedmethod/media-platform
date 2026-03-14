import type { App } from "vue";
import { ApiError } from "@/lib/api-error";
import { useToast } from "@/composables/useToast";

export function setupGlobalErrorHandler(app: App) {
  const toast = useToast();

  app.config.errorHandler = (err) => {
    if (err instanceof ApiError) {
      handleApiError(err, toast);
    } else if (err instanceof Error) {
      toast.error("Unexpected Error", err.message);
    }
    console.error("[global]", err);
  };

  window.addEventListener("unhandledrejection", (event) => {
    if (event.reason instanceof ApiError) {
      handleApiError(event.reason, toast);
      event.preventDefault();
    }
  });
}

function handleApiError(err: ApiError, toast: ReturnType<typeof useToast>) {
  if (err.isUnauthorized) {
    toast.warning("Session Expired", "Please sign in again.");
  } else if (err.isForbidden) {
    toast.warning("Access Denied", "You are not authorized for this action.");
  } else if (err.isRateLimited) {
    toast.warning("Too Many Requests", "Please wait a moment and try again.");
  } else if (err.isValidation) {
    const detail =
      err.body.detail ??
      Object.values(err.body.errors ?? {})
        .flat()
        .join("; ");
    toast.error("Validation Error", detail || err.message);
  } else if (err.isServerError) {
    toast.error(
      "Server Error",
      "Something went wrong. Please try again later.",
    );
  } else {
    toast.error("Request Failed", err.message);
  }
}
