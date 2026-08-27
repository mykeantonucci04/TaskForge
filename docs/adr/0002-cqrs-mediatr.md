# ADR 0002: CQRS Pattern with MediatR and Mapperly

## Status
Accepted

## Context
Enterprise application logic requires strict separation of concerns, maintainability, testability, and high execution performance.

## Decision
1. **CQRS Pattern**: We split operations into Commands (side-effects/state mutation) and Queries (data retrieval).
2. **MediatR**: Used as an in-process mediator for dispatching Commands and Queries. Pipeline behaviors handle cross-cutting concerns (Logging, Performance monitoring, and Validation via FluentValidation).
3. **Riok.Mapperly**: Source generator mapping framework selected over AutoMapper for zero-reflection high-performance DTO mapping.

## Consequences
### Positive
- Loose coupling between controllers/endpoints and application logic.
- Cross-cutting validation and logging handled consistently via pipeline behaviors.
- High performance DTO mapping at compile time.

### Negative
- Additional handler classes per use case.
