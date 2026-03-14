---
name: database-migration-safe
description: 'Safe database migration workflows for TypeORM (Nexus) and EF Core (.NET projects). Auto-generate only, validate against entities, rollback procedures. Use when modifying entities or database schema.'
argument-hint: Entity change description or migration issue
---

# Database Migration Safe

## When to Use

- Adding, modifying, or removing entity properties or relations.
- Creating new entities or database tables.
- Changing column types, constraints, or indexes.
- Troubleshooting migration errors or conflicts.
- Rolling back a failed migration.

## Core Principle

**All migrations MUST be auto-generated from entity definitions. Manual migration writing is strictly prohibited.**

## TypeORM (Nexus Backend)

### Generate Migration

```bash
pnpm run migration:generate -- libs/persistence/src/migrations/MigrationName
```

### Apply Migration

```bash
pnpm run migration:run
```

### Revert Last Migration

```bash
pnpm run migration:revert
```

### Workflow

1. **Modify entity** — Change properties, decorators, relations in domain entity files.
2. **Generate migration** — Run generate command. TypeORM diffs entity metadata vs current schema.
3. **Review generated file** — Verify SQL matches your intent. Do NOT edit the generated SQL.
4. **Update exports** — Add migration class to `libs/persistence/src/migrations/index.ts`.
5. **Apply migration** — Run migration:run to update the database.
6. **Test** — Verify application starts and affected queries work.

### Prohibited Actions

- ❌ Writing SQL statements manually in migration files.
- ❌ Modifying auto-generated migration files after creation.
- ❌ Deleting migration files that have been applied to any environment.
- ❌ Changing entity decorators without generating a corresponding migration.

### Rollback Procedure

1. Run `pnpm run migration:revert` to undo the last migration.
2. Delete the generated migration file.
3. Fix the entity definition.
4. Re-generate the migration.
5. Apply and test again.

## EF Core (.NET Projects)

### Generate Migration

```bash
dotnet ef migrations add MigrationName --project src/Project.Infrastructure --startup-project src/Project.Api
```

### Apply Migration

```bash
dotnet ef database update --project src/Project.Infrastructure --startup-project src/Project.Api
```

### Remove Last Migration (Unapplied)

```bash
dotnet ef migrations remove --project src/Project.Infrastructure --startup-project src/Project.Api
```

### Workflow

1. **Modify entity** — Change properties, value objects, or navigation properties.
2. **Update configuration** — Adjust `IEntityTypeConfiguration<T>` if needed (Fluent API).
3. **Generate migration** — Run `migrations add`. EF Core diffs model snapshot vs entities.
4. **Review generated file** — Check `Up()` and `Down()` methods match intent.
5. **Apply migration** — Run `database update`.
6. **Test** — Verify CRUD operations and queries work correctly.

### Prohibited Actions

- ❌ Manually writing SQL in `Up()` or `Down()` methods.
- ❌ Modifying auto-generated migrations after creation.
- ❌ Using `Database.EnsureCreated()` in production code.
- ❌ Mixing Fluent API and Data Annotations for the same property.

### Rollback Procedure

1. Run `database update PreviousMigrationName` to revert to a specific point.
2. Run `migrations remove` to delete the unapplied migration.
3. Fix the entity/configuration.
4. Re-generate and apply.

## Pre-Migration Checklist

- [ ] Entity changes are complete and consistent.
- [ ] No breaking changes to existing data (or data migration plan exists).
- [ ] Related entities/foreign keys are updated together.
- [ ] Default values specified for new non-nullable columns.
- [ ] Migration generated (not hand-written).
- [ ] Generated SQL reviewed and verified.
- [ ] Migration applied and tested locally.
- [ ] Application starts and CRUD operations work.

## Common Pitfalls

| Pitfall | Prevention |
|---------|------------|
| Forgetting migration export (TypeORM) | Always update `migrations/index.ts` |
| Enum column type mismatch | Use string enums consistently |
| Nullable → Non-nullable without default | Provide default value or data migration |
| Removing column with existing data | Add deprecation migration first |
| Concurrent migration conflicts | Coordinate with team; rebase and regenerate |
