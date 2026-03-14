import { defineConfig } from 'orval'

export default defineConfig({
  mediaPlatform: {
    input: {
      target: 'http://localhost:5000/openapi/v1.json',
    },
    output: {
      mode: 'tags-split',
      target: 'src/generated',
      schemas: 'src/generated/models',
      client: 'vue-query',
      prettier: true,
      override: {
        mutator: {
          path: 'src/composables/useApi.ts',
          name: 'apiFetch',
        },
        query: {
          useQuery: true,
          useMutation: true,
        },
      },
    },
  },
})
