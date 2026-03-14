# AI-Orchestration Skills Catalog

Denne mappe indeholder genbrugelige skills til dit multi-agent workflow.

## Custom Skills

- `implement-from-plan`  
  Implementér planlagte ændringer sikkert med minimale patches.

- `bugfix-root-cause-minimal`  
  Root-cause bugfix flow med mindst mulig ændring og test-verificering.

- `analyze-controller-status-patterns`  
  Analyse af statusmønstre (fx `this.setStatus`) og refactor-forslag.

- `test-and-validate-changes`  
  Struktureret validering af ændringer (errors/tests/build/lint).

- `implementation-report-format`  
  Ensartet outputformat: summary, files, patch, explanation.

- `write-coding-standards-from-file`  
  Udled og skriv coding standards ud fra kodefiler.

## Project-Specific Skills

- `vue-component-patterns`  
  Vue 3 Composition API patterns: composables, state management (Pinia vs TanStack Query), shadcn-vue, fil/folder-størrelses-enforcement. Til CaddyAdmin.Web og NexusFrontend.

- `clean-architecture-dotnet`  
  Clean Architecture for .NET 10: WolverineFx CQRS, EF Core, FluentValidation, Roslyn analyzers, CSharpier. Til CaddyAdmin.Api, VaultFacade og ArcaneVaultBridge.

- `database-migration-safe`  
  Sikre migrations-workflows for TypeORM (Nexus) og EF Core (.NET). Auto-genereret only, validering, rollback.

- `security-audit`  
  Applikationssikkerhed: secrets-håndtering, injection-forebyggelse, auth/authz, CORS, OWASP Top 10. Især til VaultFacade og Keycloak-integrerede services.

- `test-generation`  
  Generer unit-, integration- og komponent-tests fra kode eller krav. Jest/Vitest (TypeScript), xUnit (.NET), Vue Test Utils.

- `api-contract-versioning`  
  Håndter API-kontrakt-ændringer mellem backend og frontend: OpenAPI spec, Orval-regenerering, breaking vs non-breaking changes, deprecation.

## Imported from awesome-copilot

- `structured-autonomy-plan`  
  Official planning workflow for commit-oriented implementation plans.

- `structured-autonomy-implement`  
  Official implementation workflow that follows plan steps exactly.

- `refactor-plan`  
  Official multi-file refactor planning with sequencing and rollback.

- `agent-governance`  
  Official governance patterns for multi-agent safety, trust, and auditability.

- `git-commit`  
  Conventional commits med diff-analyse, staging og sikkerhedsprotokol.

- `structured-autonomy-generate`  
  Konverterer planer til komplette copy-paste implementeringsdokumenter.

- `context-map`  
  Pre-implementering fil-dependency og impact-mapping.

- `review-and-refactor`  
  Læser copilot-instructions og refaktorerer kode til at matche.

- `multi-stage-dockerfile`  
  Docker best practices: multi-stage builds, lag-optimering, sikkerhed.

- `postgresql-optimization`  
  Query-optimering, indexes, JSONB, monitoring, extensions.

- `create-architectural-decision-record`  
  Struktureret ADR-generering med skabelon og begrundelse.

---

## Imported Agents (`.github/agents/`)

Custom agent-personas med dedikerede modeller og ekspertise. Brug via `@agent-name` i chat.

### Tier 1 — Direkte relevante

- `vuejs-expert` — Vue 3 ekspert (Composition API, Pinia, Vitest, Playwright). Model: Claude Sonnet 4.5
- `expert-dotnet-software-engineer` — .NET ekspert med SOLID, TDD, CQRS, Security
- `se-security-reviewer` — OWASP Top 10 + Zero Trust + LLM code security review
- `debug` — Systematisk 4-fase debugging: Assessment → Investigation → Resolution → QA
- `context-architect` — Context mapping for multi-fil ændringer og dependency graphs
- `tech-debt-remediation-plan` — Tech debt analyse med Ease/Impact/Risk scores og GitHub issue integration

### Tier 2 — Nyttige

- `se-system-architecture-reviewer` — Arkitektur review med Well-Architected Framework
- `github-actions-expert` — Security-first CI/CD: OIDC auth, action pinning, supply chain safety
- `postgresql-dba` — PostgreSQL DBA med pgsql extension tools

## Imported Instructions (`.github/instructions/`)

Automatiske regler der aktiveres for filer der matcher `applyTo` pattern. Distribueret til relevante projekter.

### Tier 1 — Stack-specifikke

- `nestjs.instructions.md` — NestJS: DI, modules, TypeORM, JWT auth, testing → Nexus
- `vuejs3.instructions.md` — Vue 3: Composition API, Pinia, Vitest, Tailwind → CaddyAdmin.Web, NexusFrontend
- `csharp.instructions.md` — C# 14: EF Core, nullable refs, validation, testing → CaddyAdmin.Api, vaultfacade, arcane-vault-bridge
- `security-and-owasp.instructions.md` — OWASP Top 10: injection, XSS, SSRF, crypto → Alle projekter

### Tier 2 — Universelle

- `containerization-docker-best-practices.instructions.md` — Multi-stage builds, sikkerhed, layer-optimering → Alle med Dockerfiles
- `github-actions-ci-cd-best-practices.instructions.md` — Workflows, secrets, caching, deployment → CI/CD pipelines

## Imported Hooks (`.github/hooks/`)

Shell-script hooks der kører automatisk ved Copilot session events.

- `governance-audit` — Scanner prompts for threat patterns (data exfil, privilege escalation, prompt injection). JSON audit log med governance levels: open/standard/strict/locked
