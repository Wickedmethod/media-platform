import { defineConfig } from "orval";

export default defineConfig({
  mediaPlatform: {
    input: {
      target: "./openapi.json",
    },
    output: {
      mode: "tags-split",
      target: "src/generated",
      schemas: "src/generated/models",
      client: "vue-query",
      prettier: true,
      override: {
        mutator: {
          path: "src/lib/api-client.ts",
          name: "apiClient",
        },
        query: {
          useQuery: true,
          useMutation: true,
        },
      },
    },
  },
});
