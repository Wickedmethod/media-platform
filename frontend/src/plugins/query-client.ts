import { QueryClient, type VueQueryPluginOptions } from '@tanstack/vue-query'
import { ApiError } from '@/lib/api-error'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10_000,
      retry: (failureCount, error) => {
        if (error instanceof ApiError && (error.isUnauthorized || error.isForbidden || error.isNotFound)) {
          return false
        }
        return failureCount < 3
      },
    },
    mutations: {
      retry: false,
    },
  },
})

export const vueQueryOptions: VueQueryPluginOptions = {
  queryClient,
}
