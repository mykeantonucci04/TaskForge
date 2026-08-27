# TaskForge API Contract Specification

## OpenAPI Version: 3.1.0

### Authentication
All requests except `/api/v1/auth/*` and `/health` require standard HTTP Bearer JWT token.

### Endpoints Overview

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/health` | Health check endpoint for DB & services | No |
| `POST` | `/api/v1/auth/login` | Authenticate user and issue JWT + Refresh Token | No |
| `POST` | `/api/v1/tenants` | Create new tenant schema | Yes (`SystemAdmin`) |
| `GET`  | `/api/v1/tenants/{id}` | Get tenant metadata by ID | Yes |
| `POST` | `/api/v1/workspaces` | Create new workspace within current tenant | Yes |
| `GET`  | `/api/v1/workspaces` | List workspaces for active tenant | Yes |
| `POST` | `/api/v1/tasks` | Create task item in column | Yes |
| `GET`  | `/api/v1/tasks/column/{columnId}` | List tasks for specific column | Yes |
| `PATCH`| `/api/v1/tasks/{id}/status` | Update status of a task | Yes |

### Header Parameters

- `Authorization`: `Bearer <jwt_token>`
- `X-Tenant-ID`: `<tenant_identifier>` (Optional if `tenant_id` claim is present in JWT)

### Error Response Format

Standard `ProblemDetails` (`application/problem+json`):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "errors": {
    "Email": ["Email address is required."]
  }
}
```
