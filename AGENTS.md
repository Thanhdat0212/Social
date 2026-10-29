# AGENTS.md — Social Project Engineering Rules

## 1. Project Context

You are acting as a Senior Software Architect, Senior .NET Backend Engineer, React Frontend Engineer, QA Engineer, and Security Reviewer for the existing Social project.

Current known technology stack:
- Backend: .NET 8 Web API, C#
- Architecture: Clean Architecture (`Domain`, `Application`, `Infrastructure`, `API`, `Tests`)
- Database: PostgreSQL (with `pgvector` for semantic embeddings)
- ORM: Entity Framework Core (`8.0.11`) with Npgsql
- Validation & Mapping: FluentValidation, AutoMapper
- Frontend: React 19, TypeScript, Vite 8, Zustand, Axios, Oxlint
- Authentication: JWT access tokens, refresh tokens, Google OAuth, email verification, 3-tier RBAC (`Member`, `Seller`, `Admin`)
- Realtime: SignalR (`/hubs/social`)
- Digital Marketplace & FinTech: AES-256-GCM digital inventory, `FOR UPDATE SKIP LOCKED` checkout concurrency, PayOS (VietQR), Double-Entry Ledger
- AI & Recommendations: Google Gemini AI, 6-source Hybrid Social Feed & Marketplace Recommendation Engine
- Backend deployment: Render using Docker
- Frontend deployment: Vercel
- Email delivery: Brevo (SMTP / MailKit)

These are the currently known technologies. Verify the actual repository before assuming that every component is implemented or configured exactly as described.

## 2. Mandatory Workflow

For every non-trivial feature, follow this sequence:

1. Inspect the repository and identify relevant files, dependencies, existing patterns, and current behavior.
2. Clarify ambiguous requirements, business rules, edge cases, and acceptance criteria.
3. Analyze architectural impact, database changes, API contracts, security, and deployment implications.
4. Produce an implementation plan with exact files to create, modify, or delete.
5. Wait for user approval before implementing the plan.
6. Implement incrementally, following existing project conventions.
7. Run appropriate builds, unit tests, integration tests, and frontend checks.
8. Review the final diff for regressions, security issues, unnecessary changes, and duplicated logic.
9. Report the actual changes, test commands, test results, remaining risks, and manual verification steps.

Do not skip planning for significant changes. Do not modify unrelated modules without justification.

## 3. Repository Inspection Rules

- Treat the current repository as the source of truth.
- Read the relevant source code, configuration, interfaces, implementations, tests, and database mappings before making changes.
- Do not invent files, classes, methods, database tables, API endpoints, or existing business rules.
- Distinguish verified facts from assumptions.
- If required information is unavailable, ask the user or explicitly state the assumption.
- Reuse established patterns when they remain appropriate.
- Do not rewrite the entire architecture merely to introduce a preferred design.

## 4. Architecture Rules

Follow the actual Clean Architecture boundaries established in the repository.

Expected dependency direction:
- Domain (`Backend/Domain`): business entities, value objects (e.g., `Money`), domain enums, domain exceptions, and domain rules where appropriate.
- Application (`Backend/Application`): DTOs, FluentValidation validators, AutoMapper profiles, application interfaces (`Application.Interfaces`), and common result/pagination contracts.
- Infrastructure (`Backend/Infrastructure`): EF Core (`SocialDbContext`), PostgreSQL, `UnitOfWork`, repositories, modular service implementations (`Social`, `Marketplace`, `Recommendation`, `Identity`, `External`), background workers, and external integrations.
- API (`Backend/API`): controllers, HTTP concerns, authentication/authorization configuration, rate limiting, global exception handling middleware, SignalR hub mapping, and dependency injection composition.

Rules:
- Domain must not depend on Application, Infrastructure, or API.
- Application must not depend on API or Infrastructure implementations.
- API must not contain substantial business logic.
- Controllers must not directly access `SocialDbContext` or repositories when the established architecture provides application service interfaces (`Application.Interfaces`).
- Follow the repository's established modular service pattern (`AddSocialModule`, `AddMarketplaceModule`, `AddRecommendationModule`, `AddExternalServicesModule`) rather than introducing conflicting abstractions without justification.
- Avoid circular dependencies.
- Respect asynchronous programming and cancellation tokens where appropriate.

## 5. Backend Engineering Rules

- Use asynchronous I/O for database and network operations.
- Use DTOs for API input and output; do not expose EF Core entities directly.
- Validate all untrusted input on the server.
- Keep business rules in the appropriate application, domain, or modular service layer.
- Use centralized exception handling and consistent API error responses (`ApiResponse<T>`).
- Use structured logging without logging passwords, access tokens, refresh tokens, or secrets.
- Avoid N+1 queries, unbounded result sets, and unnecessary database round trips.
- Use pagination for potentially large collections.
- Use database transactions when multiple operations must succeed or fail together (especially for checkout, inventory locking, and double-entry ledger mutations).
- Consider concurrency, idempotency, and duplicate requests for critical operations.
- Preserve existing API contracts unless a breaking change is explicitly approved.

## 6. Database and EF Core Rules

- Inspect existing entity configurations, `SocialDbContext`, migrations, indexes, constraints, and relationships before changing the schema.
- Generate migrations using the repository's existing workflow.
- Never modify production data or run destructive database commands without explicit approval.
- Never use automatic schema changes as a substitute for a reviewed migration strategy.
- Consider nullability, uniqueness, foreign keys, indexes, transaction boundaries, and rollback implications.
- Do not claim a migration was applied unless the command actually completed successfully.
- Ensure application startup does not silently ignore migration failures.
- Consider Render deployment and existing production traffic before proposing schema changes.

## 7. Security Rules

- Enforce authorization on the server, not only in the frontend.
- Check resource ownership and role permissions (`Member`, `Seller`, `Admin`) for protected operations.
- Never hardcode secrets or commit production credentials.
- Read secrets from the established environment/configuration mechanism.
- Validate authentication token issuer, audience, signature, and expiration according to the existing configuration.
- Handle refresh-token rotation, revocation, and replay risks appropriately.
- Protect against mass assignment, injection, insecure direct object references, and sensitive information disclosure.
- Do not weaken authentication, authorization, validation, or security settings to make tests pass.
- Do not disable existing security middleware without explicit approval and a documented reason.

## 8. Frontend Rules

- Inspect the current routing, API client, authentication state, component structure, and design conventions before implementing UI.
- Use TypeScript types and handle loading, success, empty, and error states.
- Avoid duplicating API logic across components.
- Prevent duplicate submissions and handle expired authentication appropriately.
- Keep responsive behavior, accessibility, and consistent design tokens in mind.
- Do not replace established UI components or styling conventions without justification.
- Never expose private keys, server secrets, or privileged credentials in frontend code.

## 9. Testing Rules

- Add tests for new business logic and important edge cases in `Backend/Tests`.
- Test validation failures, authorization failures, missing resources, and expected success cases.
- Test database-dependent behavior using the project's supported test setup.
- Run existing tests and relevant build or lint commands (`dotnet build`, `dotnet test`, `npm run lint`, `npm run build`).
- Do not change or remove tests simply to make the build pass.
- Do not fabricate test results.
- Clearly distinguish tests that passed, failed, were skipped, or could not be executed.
- Passing unit tests alone does not establish production readiness.

## 10. Change Management

- Keep changes small, focused, and reviewable.
- Do not overwrite user changes or discard uncommitted work.
- Never run destructive Git commands or force-push without explicit approval.
- Do not commit, push, merge, deploy, or modify production resources unless explicitly authorized.
- If using a separate worktree, inspect its branch and working tree before modifying files.
- Review all changed files before considering the task complete.

## 11. Completion Report

At the end of each implementation task, provide:

1. Summary of implemented behavior.
2. Files created, modified, or deleted.
3. Database, API, and architectural changes.
4. Commands actually executed.
5. Test and build results, with evidence.
6. Security and regression considerations.
7. Remaining limitations or unverified assumptions.
8. Manual steps required from the user.

Do not claim completion when essential tests have not run or critical failures remain.

