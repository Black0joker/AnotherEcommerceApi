# E-Commerce API — ASP.NET Core 10 + SQL Server

## 1. Project Goal

Build a production-style e-commerce REST API using:

- ASP.NET Core 10 Web API
- C#
- SQL Server
- Entity Framework Core 10
- ASP.NET Core Identity
- JWT Bearer Authentication
- Redis
- File/Object Storage
- Background Jobs
- Structured Logging
- OpenAPI/Swagger
- Docker
- Automated Tests

The goal is not simply to build an API that works. The project should demonstrate practical backend engineering: architecture, security, database design, transactions, concurrency, caching, testing, observability, and deployment.

---

## 2. Target Architecture

Use a modular monolith with Clean Architecture/layered boundaries.

```text
                    ┌──────────────────────────┐
                    │       Client Apps        │
                    │ Web / Mobile / Swagger   │
                    └────────────┬─────────────┘
                                 │ HTTP
                                 ▼
                    ┌──────────────────────────┐
                    │     E-Commerce API       │
                    │    ASP.NET Core 10       │
                    └────────────┬─────────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              │                  │                  │
              ▼                  ▼                  ▼
       ┌────────────┐      ┌───────────┐     ┌──────────────┐
       │ SQL Server │      │   Redis   │     │ File Storage │
       │            │      │           │     │              │
       │ EF Core    │      │ Cache     │     │ Product      │
       │ Identity   │      │           │     │ Images       │
       └────────────┘      └───────────┘     └──────────────┘
              │
              ▼
       Background Jobs
       Logging / Events
```

Recommended solution:

```text
ECommerce.sln

src/
├── ECommerce.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Filters/
│   ├── Extensions/
│   ├── Configuration/
│   └── Program.cs
│
├── ECommerce.Application/
│   ├── Abstractions/
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Products/
│   │   ├── Categories/
│   │   ├── Cart/
│   │   ├── Wishlist/
│   │   ├── Orders/
│   │   ├── Reviews/
│   │   ├── Discounts/
│   │   └── Admin/
│   ├── DTOs/
│   ├── Validators/
│   ├── Behaviors/
│   └── Common/
│
├── ECommerce.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── ValueObjects/
│   ├── Events/
│   ├── Exceptions/
│   └── Common/
│
└── ECommerce.Infrastructure/
    ├── Persistence/
    │   ├── Context/
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── Repositories/
    ├── Identity/
    ├── Caching/
    ├── Storage/
    ├── BackgroundJobs/
    └── Services/

tests/
├── ECommerce.UnitTests/
├── ECommerce.IntegrationTests/
└── ECommerce.ArchitectureTests/
```

### Dependency Direction

```text
Api
 │
 ▼
Application
 │
 ▼
Domain

Infrastructure ───────► Application
Infrastructure ───────► Domain
```

The Domain layer should not depend on Infrastructure or ASP.NET Core.

---

# 3. Phase 0 — Requirements and Business Rules

Before writing implementation code, define:

- Customer capabilities
- Admin capabilities
- Authentication rules
- Product rules
- Category rules
- Inventory rules
- Cart rules
- Wishlist rules
- Checkout rules
- Order lifecycle
- Discount rules
- Review rules
- File-upload rules
- Authorization rules
- Error-handling rules

## Order Lifecycle

```text
Pending
   │
   ▼
Confirmed
   │
   ▼
Processing
   │
   ▼
Shipped
   │
   ▼
Delivered
```

Possible cancellation paths:

```text
Pending ─────► Cancelled
Confirmed ───► Cancelled
```

Define these business rules before implementing checkout.

---

# 4. Phase 1 — Create the Solution

Create:

- `ECommerce.Api`
- `ECommerce.Application`
- `ECommerce.Domain`
- `ECommerce.Infrastructure`
- `ECommerce.UnitTests`
- `ECommerce.IntegrationTests`
- `ECommerce.ArchitectureTests`

Configure project references according to the dependency direction.

The first milestone should compile with a clean architecture skeleton.

Do not begin with controllers.

---

# 5. Phase 2 — Domain Modeling

## Identity

Use ASP.NET Core Identity and extend the user entity for e-commerce-specific information.

Core concepts:

```text
User
Role
RefreshToken
Address
```

## Catalog

```text
Product
Category
ProductCategory
ProductImage
```

Potential `Product` fields:

```text
Id
Name
Slug
Description
SKU
Price
CompareAtPrice
IsActive
CreatedAt
UpdatedAt
RowVersion
```

## Inventory

```text
InventoryItem
InventoryTransaction
```

Important concepts:

```text
AvailableQuantity
ReservedQuantity
```

## Cart

```text
Cart
CartItem
```

## Wishlist

```text
Wishlist
WishlistItem
```

## Orders

```text
Order
OrderItem
OrderAddress
Payment
```

`OrderItem` should preserve historical information:

```text
ProductId
ProductName
SKU
UnitPrice
Quantity
DiscountAmount
Total
```

Do not rely entirely on the current Product record to reconstruct old orders.

## Discounts

```text
Discount
DiscountProduct
DiscountCategory
```

Support:

- Percentage discounts
- Fixed discounts
- Start/end dates
- Usage limits
- Minimum order value
- Active/inactive state

## Reviews

```text
Review
```

Potential fields:

```text
ProductId
UserId
Rating
Title
Comment
CreatedAt
UpdatedAt
IsApproved
```

Consider enforcing one review per customer per product.

---

# 6. Phase 3 — EF Core + SQL Server

Configure:

- EF Core
- SQL Server provider
- `DbContext`
- Fluent API
- Entity configurations
- Relationships
- Constraints
- Indexes
- Migrations
- Concurrency tokens

Keep entity configuration under:

```text
ECommerce.Infrastructure/
└── Persistence/
    └── Configurations/
```

Example configurations:

```text
ProductConfiguration
OrderConfiguration
OrderItemConfiguration
CartConfiguration
ReviewConfiguration
...
```

---

# 7. Database Design

Create indexes around actual query patterns.

Important candidates:

```text
Product.SKU
Product.Slug
Product.IsActive

Order.UserId
Order.Status
Order.CreatedAt

Review.ProductId
Review.UserId
```

For filtering/searching, add indexes based on measured access patterns.

Do not index every column.

Add database constraints for invariants that should never be violated.

Examples:

- Unique SKU
- Unique product slug
- Unique wishlist item per user/product
- Appropriate foreign keys
- Positive quantity constraints where appropriate

---

# 8. Optimistic Concurrency

Use SQL Server `rowversion` for entities that are vulnerable to concurrent updates.

Example:

```text
Client A reads inventory
        │
        ▼
Version = 5

Client B reads inventory
        │
        ▼
Version = 5

Client A updates
        │
        ▼
Version = 6

Client B tries to update
        │
        ▼
Concurrency conflict
```

The API should detect the conflict and return an appropriate conflict response rather than silently overwriting another update.

This is particularly important for inventory.

---

# 9. Phase 4 — Application Layer

Organize application functionality by feature.

Example:

```text
Features/
└── Products/
    ├── CreateProduct/
    ├── UpdateProduct/
    ├── DeleteProduct/
    ├── GetProduct/
    ├── GetProducts/
    └── SearchProducts/
```

Preferred flow:

```text
HTTP Request
     ↓
Validation
     ↓
Application Handler/Service
     ↓
Domain Rules
     ↓
Infrastructure
     ↓
Database
```

Keep controllers thin.

Controllers should primarily handle:

- HTTP concerns
- Request binding
- Authentication context
- Calling application logic
- Response/status-code mapping

Business logic should not live in controllers.

---

# 10. Phase 5 — Authentication

Implement:

- Registration
- Login
- Access tokens
- Refresh tokens
- Refresh-token rotation
- Logout/revocation
- Current-user endpoint
- Roles

Use:

- ASP.NET Core Identity
- JWT Bearer authentication
- Identity password hashing
- Secure refresh-token storage

Do not implement password hashing yourself.

---

# 11. JWT Design

Access tokens can contain:

```text
sub
email
role
jti
```

Use relatively short-lived access tokens.

Use refresh tokens to obtain new access tokens.

Flow:

```text
Login
  │
  ▼
Access Token + Refresh Token
  │
  ├── Access token expires
  │
  ▼
Refresh endpoint
  │
  ▼
New Access Token
```

Implement refresh-token revocation/rotation rather than treating refresh tokens as permanent credentials.

---

# 12. Role-Based Authorization

Start with:

```text
Customer
Admin
```

Later you may introduce more specialized roles if needed.

Examples:

```text
GET /api/v1/products
```

Public.

```text
POST /api/v1/products
```

Admin.

```text
GET /api/v1/orders
```

Authenticated customer; only their own orders.

```text
GET /api/v1/admin/orders
```

Admin.

Authorization must always be enforced server-side.

---

# 13. Phase 6 — Product Catalog

Implement the catalog before cart and checkout because those features depend on products.

## Product Endpoints

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
GET    /api/v1/products/{id}/related
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

## Category Endpoints

```text
GET    /api/v1/categories
GET    /api/v1/categories/{id}
POST   /api/v1/categories
PUT    /api/v1/categories/{id}
DELETE /api/v1/categories/{id}
```

---

# 14. Pagination

Never return an unbounded product collection.

Example:

```text
GET /api/v1/products?page=2&pageSize=20
```

Response:

```json
{
  "items": [],
  "page": 2,
  "pageSize": 20,
  "totalCount": 245,
  "totalPages": 13
}
```

Set a maximum page size, for example 100.

Do not allow clients to request arbitrary amounts of data.

---

# 15. Filtering

Support filters such as:

```text
categoryId
minPrice
maxPrice
minRating
maxRating
isAvailable
```

Example:

```text
GET /api/v1/products?categoryId=3&minPrice=20&maxPrice=100
```

Filtering must happen in SQL through EF Core rather than loading all records into memory.

---

# 16. Sorting

Support a controlled set of sort options:

```text
price_asc
price_desc
name_asc
name_desc
newest
rating
```

Do not accept arbitrary SQL/order expressions from clients.

---

# 17. Search

Start with database-backed search:

```text
GET /api/v1/products?search=keyboard
```

Search fields can initially include:

```text
Name
Description
SKU
```

If the catalog grows, investigate SQL Server Full-Text Search before adding a separate search engine.

---

# 18. Phase 7 — Redis Caching

Introduce Redis after the basic catalog works.

Good initial cache candidates:

```text
Product details
Categories
Product listing results
```

Use a cache-aside strategy:

```text
Request
  │
  ▼
Redis cache?
 /      \
yes      no
│         │
▼         ▼
return   SQL Server
            │
            ▼
          Redis
            │
            ▼
          return
```

When changing a product:

```text
Update SQL Server
       │
       ▼
Invalidate product cache
```

Do not cache everything.

Design cache keys carefully, especially for user-specific data.

---

# 19. Phase 8 — Cart

Endpoints:

```text
GET    /api/v1/cart
POST   /api/v1/cart/items
PUT    /api/v1/cart/items/{productId}
DELETE /api/v1/cart/items/{productId}
DELETE /api/v1/cart
```

Rules:

- Product must exist.
- Product must be active.
- Quantity must be positive.
- Quantity must respect inventory.
- Prices are calculated server-side.
- Client-supplied totals are never trusted.

The client should never control the final order total.

---

# 20. Phase 9 — Wishlist

Endpoints:

```text
GET    /api/v1/wishlist
POST   /api/v1/wishlist/items/{productId}
DELETE /api/v1/wishlist/items/{productId}
```

Add a unique database constraint so one user cannot have the same product twice in their wishlist.

---

# 21. Phase 10 — Inventory

Do not model inventory as only:

```text
Product.Stock
```

Use an inventory model:

```text
InventoryItem
----------------
ProductId
AvailableQuantity
ReservedQuantity
```

And an inventory ledger:

```text
InventoryTransaction
--------------------
Id
ProductId
Type
Quantity
Reference
CreatedAt
```

Transaction types can include:

```text
Purchase
Reservation
Release
Adjustment
Return
```

This creates an audit trail.

---

# 22. Phase 11 — Checkout

Checkout is the most important transactional workflow.

Flow:

```text
Checkout
   │
   ├── Validate cart
   │
   ├── Load authoritative product data
   │
   ├── Validate prices
   │
   ├── Validate discounts
   │
   ├── Validate inventory
   │
   ├── Reserve/decrement inventory
   │
   ├── Create Order
   │
   ├── Create OrderItems
   │
   ├── Calculate totals
   │
   ├── Clear cart
   │
   └── Commit transaction
```

If any database operation fails:

```text
ROLLBACK
```

The database must not end up with half of the checkout applied.

---

# 23. Transactions

Use EF Core/database transactions when several database operations must succeed together.

For example:

```text
Inventory update
       +
Order creation
       +
Order items
       +
Cart cleanup
```

should be atomic.

Understand the distinction between a database transaction and operations involving external systems.

A SQL Server transaction cannot roll back an external payment provider or email provider.

For external operations, learn:

- Idempotency
- Outbox pattern
- Retry strategies
- Eventual consistency

---

# 24. Checkout Idempotency

Prevent duplicate orders caused by client retries.

Example:

```text
POST /api/v1/orders/checkout
Idempotency-Key: abc123
```

If the same key is submitted again, return the existing checkout result instead of creating another order.

Store idempotency records in SQL Server and enforce uniqueness on the key within the appropriate scope.

---

# 25. Phase 12 — Orders

Customer endpoints:

```text
GET  /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/orders/checkout
POST /api/v1/orders/{id}/cancel
```

Admin endpoints:

```text
GET /api/v1/admin/orders
GET /api/v1/admin/orders/{id}
PUT /api/v1/admin/orders/{id}/status
```

Customers must only be able to access their own orders.

This protects against IDOR/resource-ownership vulnerabilities.

---

# 26. Phase 13 — Reviews

Endpoints:

```text
GET    /api/v1/products/{productId}/reviews
POST   /api/v1/products/{productId}/reviews
PUT    /api/v1/reviews/{id}
DELETE /api/v1/reviews/{id}
```

Recommended rule:

> A customer can review a product only after purchasing it.

Test this as a business rule, not merely as controller validation.

---

# 27. Phase 14 — File Uploads

Create a storage abstraction:

```text
IFileStorage
```

Start with:

```text
LocalFileStorage
```

Later you can implement:

```text
S3FileStorage
AzureBlobStorage
```

Store file metadata in SQL Server:

```text
ProductImage
----------------
Id
ProductId
StorageKey
FileName
ContentType
Size
Url
CreatedAt
```

Validate:

- Maximum file size
- MIME type
- Extension
- Filename
- Image dimensions where appropriate

Never blindly trust an uploaded filename or content type.

---

# 28. Phase 15 — Background Jobs

Use background processing for work that should not block HTTP requests.

Examples:

```text
Send order confirmation
Generate reports
Clean expired carts
Expire discounts
Clean expired refresh tokens
Resize product images
Send notifications
```

Architecture:

```text
API
 │
 ▼
Job Queue
 │
 ▼
Background Worker
 │
 ├── Email
 ├── Reports
 └── Cleanup
```

Start with ASP.NET Core hosted background services to learn the fundamentals. Later, introduce a durable job system if the project requires stronger reliability.

---

# 29. Phase 16 — Discounts

Create a dedicated pricing/discount component.

For example:

```text
IDiscountCalculator
```

The server should calculate:

```text
Subtotal
- Discount
+ Tax
+ Shipping
= Grand Total
```

Never trust totals supplied by the client.

Support:

- Percentage discounts
- Fixed discounts
- Expiration
- Usage limits
- Minimum order values
- Product/category restrictions
- Active/inactive state

---

# 30. Phase 17 — Admin API

## Products

```text
Create
Update
Delete/deactivate
Upload images
```

## Categories

```text
Create
Update
Delete
```

## Inventory

```text
View stock
Adjust stock
View inventory history
```

## Orders

```text
View all orders
Search
Filter
Update status
```

## Users

```text
List users
View user
Deactivate user
Change role
```

## Discounts

```text
Create
Update
Delete
Activate
Deactivate
```

## Reports

```text
Sales
Orders
Revenue
Products
Inventory
Customers
```

---

# 31. Phase 18 — Error Handling

Implement global exception handling.

Use a consistent Problem Details response.

Example:

```json
{
  "type": "https://example.com/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": [
      "Email is invalid."
    ]
  }
}
```

Do not return stack traces or internal exception details to clients.

Log detailed diagnostic information internally.

---

# 32. Validation

Separate two kinds of validation.

## Input Validation

Examples:

```text
Invalid email
Negative quantity
Invalid price
Missing product name
Invalid pagination parameters
```

## Business Validation

Examples:

```text
Product is inactive
Insufficient inventory
Discount has expired
Customer has not purchased product
Order cannot be cancelled
```

Both should be tested.

---

# 33. DTOs

Never expose EF Core entities directly as API contracts.

Use separate request/response models:

```text
ProductDto
ProductDetailsDto
CreateProductRequest
UpdateProductRequest
OrderDto
OrderItemDto
CreateReviewRequest
...
```

Benefits:

- Prevent over-posting
- Protect database structure
- Better API contracts
- Easier versioning
- Easier validation

---

# 34. API Versioning

Use a versioned API contract from the beginning.

For example:

```text
/api/v1/products
/api/v1/orders
/api/v1/cart
```

Keep the API contract independent from your internal domain model.

---

# 35. Structured Logging

Use structured logs rather than unstructured strings.

Important properties:

```text
RequestId
CorrelationId
UserId
OrderId
ProductId
```

Useful business events:

```text
UserRegistered
UserLoggedIn
ProductCreated
ProductUpdated
CheckoutStarted
OrderCreated
PaymentFailed
InventoryConflict
OrderCancelled
```

Never log:

- Passwords
- JWTs
- Refresh tokens
- Sensitive payment data

---

# 36. Correlation IDs

Every request should have a request/correlation identifier.

Conceptually:

```text
HTTP Request
     │
     ├── API logs
     ├── Database-related logs
     ├── Background job logs
     └── Error logs
```

This makes production debugging much easier.

---

# 37. Health Checks

Add:

```text
/health
/health/ready
```

Check important dependencies:

```text
ASP.NET Core API
SQL Server
Redis
File Storage
```

Use readiness checks to distinguish "application process is alive" from "application can serve requests."

---

# 38. Security Checklist

Implement:

- HTTPS
- JWT validation
- Role-based authorization
- Resource ownership authorization
- Strong password policy
- Refresh-token revocation
- Input validation
- File-upload validation
- Rate limiting
- CORS configuration
- Secure configuration/secrets
- SQL parameterization through EF Core
- Protection against over-posting
- Consistent authorization checks

Test for:

```text
IDOR
Privilege escalation
Unauthorized product modification
Unauthorized order access
Mass assignment
Invalid file uploads
Authentication bypass
```

---

# 39. Rate Limiting

Add rate limiting, especially to sensitive endpoints:

```text
POST /api/v1/auth/login
POST /api/v1/auth/register
POST /api/v1/auth/refresh
POST /api/v1/reviews
```

Make limits configurable.

---

# 40. Testing Strategy

Use three main levels.

## Unit Tests

Test business logic such as:

```text
DiscountCalculator
OrderCalculator
Inventory rules
Domain rules
Validation
```

## Integration Tests

Test:

```text
API
SQL Server
EF Core
Authentication
Authorization
Redis where appropriate
```

Examples:

```text
Register user
Login
Create product
Add product to cart
Checkout
Create order
```

## Architecture Tests

Verify:

```text
Domain does not reference Infrastructure
Domain does not reference API
Application does not depend on API
Controllers remain thin
```

---

# 41. Critical Test Scenarios

## Successful Checkout

```text
Cart
  ↓
Checkout
  ↓
Order created
  ↓
Inventory updated
  ↓
Cart cleared
```

## Insufficient Inventory

```text
Checkout
  ↓
Inventory unavailable
  ↓
No order created
  ↓
Cart remains
```

## Concurrency

Two customers purchase the final unit.

Expected:

```text
Customer A → succeeds
Customer B → receives conflict/failure
```

Never:

```text
Stock = -1
```

## Idempotency

Send the same checkout request multiple times using the same idempotency key.

Expected:

```text
One order
One checkout result
```

---

# 42. API Documentation

Use OpenAPI/Swagger.

Document:

- Authentication
- Endpoints
- Request models
- Response models
- Validation errors
- Authorization requirements
- Pagination
- Filtering
- Sorting
- Common status codes

The API should be understandable without reading the source code.

---

# 43. Docker

Containerize the application.

Development environment:

```text
Docker Compose
│
├── ASP.NET Core API
├── SQL Server
└── Redis
```

Aim for a simple local startup such as:

```bash
docker compose up
```

Use environment variables for connection strings and secrets.

---

# 44. Configuration

Use:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

Keep production secrets outside source control.

Typical secrets/configuration:

```text
SQL connection string
JWT configuration
Redis connection
File storage credentials
Email credentials
```

---

# 45. Database Migrations

Track schema changes with EF Core migrations.

Workflow:

```text
Change model
    ↓
Create migration
    ↓
Review migration
    ↓
Apply migration
```

Do not rely on manually changing production databases without tracking schema changes.

---

# 46. Development Seed Data

Create development seed data for:

```text
Admin user
Customer users
Categories
Products
Inventory
Discounts
```

Make it easy to launch the project and immediately exercise the API.

Never seed real production credentials.

---

# 47. CI Pipeline

After the application is stable, create CI.

Pipeline:

```text
Push
 │
 ▼
Restore
 │
 ▼
Build
 │
 ▼
Unit Tests
 │
 ▼
Integration Tests
 │
 ▼
Architecture Tests
 │
 ▼
Docker Build
```

Later add:

```text
Security scanning
Container scanning
Deployment
```

---

# 48. Recommended Development Order

Do not implement the requirements in the original bullet-list order.

Use this progression:

```text
1. Solution + architecture
        ↓
2. Domain entities
        ↓
3. EF Core + SQL Server
        ↓
4. Identity + JWT
        ↓
5. Error handling + validation
        ↓
6. Product catalog
        ↓
7. Categories
        ↓
8. Pagination/filtering/sorting/search
        ↓
9. Redis caching
        ↓
10. Cart
        ↓
11. Wishlist
        ↓
12. Inventory
        ↓
13. Checkout + transactions
        ↓
14. Orders
        ↓
15. Reviews
        ↓
16. File storage
        ↓
17. Discounts
        ↓
18. Background jobs
        ↓
19. Admin APIs
        ↓
20. Reports
        ↓
21. Logging/observability
        ↓
22. Testing
        ↓
23. Docker
        ↓
24. CI/CD
```

---

# 49. Milestones

## Milestone 1 — Foundation

Deliver:

- Solution structure
- Project references
- Dependency injection
- Configuration
- SQL Server
- EF Core
- Initial migration
- OpenAPI/Swagger
- Global error handling
- Health checks

**Goal:** Empty but professionally structured API.

---

## Milestone 2 — Authentication

Deliver:

- Registration
- Login
- JWT
- Refresh tokens
- Logout/revocation
- Roles
- Authorization policies

**Goal:** Secure API foundation.

---

## Milestone 3 — Catalog

Deliver:

- Products
- Categories
- Product details
- CRUD
- Pagination
- Filtering
- Sorting
- Search

**Goal:** Fully functional product catalog.

---

## Milestone 4 — Redis

Deliver:

- Redis connection
- Cache abstraction
- Product caching
- Category caching
- Cache invalidation

**Goal:** Understand distributed caching.

---

## Milestone 5 — Shopping

Deliver:

- Cart
- Cart items
- Wishlist
- Inventory

**Goal:** Customer shopping experience.

---

## Milestone 6 — Checkout

Deliver:

- Pricing calculation
- Discounts
- Inventory validation
- Transactions
- Order creation
- Idempotency
- Concurrency handling

**Goal:** Production-style transactional workflow.

---

## Milestone 7 — Orders & Reviews

Deliver:

- Customer order history
- Order details
- Cancellation
- Admin order management
- Reviews
- Verified-purchase reviews

---

## Milestone 8 — Files & Jobs

Deliver:

- Product image uploads
- File storage abstraction
- Background jobs
- Email notification service/simulation

---

## Milestone 9 — Admin

Deliver:

- Product management
- Category management
- Inventory management
- User management
- Discount management
- Order management
- Reports

---

## Milestone 10 — Production Hardening

Deliver:

- Structured logging
- Correlation IDs
- Rate limiting
- Health checks
- Security hardening
- Integration tests
- Docker
- CI/CD
- Production configuration

---

# 50. Recommended API Surface

```text
/api/v1/auth
    POST /register
    POST /login
    POST /refresh
    POST /logout
    GET  /me

/api/v1/products
    GET    /
    GET    /{id}
    POST   /
    PUT    /{id}
    DELETE /{id}

/api/v1/categories
    GET    /
    GET    /{id}
    POST   /
    PUT    /{id}
    DELETE /{id}

/api/v1/cart
    GET    /
    POST   /items
    PUT    /items/{productId}
    DELETE /items/{productId}
    DELETE /

/api/v1/wishlist
    GET    /
    POST   /items/{productId}
    DELETE /items/{productId}

/api/v1/orders
    GET    /
    GET    /{id}
    POST   /checkout
    POST   /{id}/cancel

/api/v1/products/{productId}/reviews
    GET    /
    POST   /

/api/v1/reviews
    PUT    /{id}
    DELETE /{id}

/api/v1/admin/products
/api/v1/admin/categories
/api/v1/admin/inventory
/api/v1/admin/orders
/api/v1/admin/users
/api/v1/admin/discounts
/api/v1/admin/reports
```

The exact endpoint design can evolve as the domain becomes clearer.

---

# 51. Definition of Done

A feature is not finished merely because an endpoint returns `200 OK`.

For each significant feature, aim for:

```text
✓ Domain model
✓ Database configuration
✓ Migration
✓ DTOs
✓ Validation
✓ Business rules
✓ Authorization
✓ Application logic
✓ API endpoint
✓ Error handling
✓ Logging
✓ Unit tests
✓ Integration tests
✓ OpenAPI documentation
```

For example, "Checkout completed" should mean:

```text
✓ Authentication
✓ Authorization
✓ Cart validation
✓ Product validation
✓ Price calculation
✓ Discount validation
✓ Inventory validation
✓ Concurrency handling
✓ Transaction
✓ Order creation
✓ Inventory update
✓ Cart cleanup
✓ Idempotency
✓ Logging
✓ Tests
```

---

# 52. Portfolio Priorities

If the purpose is to create a strong backend portfolio project, prioritize depth over the number of CRUD endpoints.

## Priority 1 — Checkout

Demonstrate:

- Transactions
- Concurrency
- Idempotency
- Inventory management

## Priority 2 — Authorization

Demonstrate:

- JWT
- Roles
- Policies
- Resource ownership

## Priority 3 — Database Design

Demonstrate:

- Relationships
- Indexes
- Constraints
- Migrations
- Concurrency tokens

## Priority 4 — Redis

Demonstrate:

- Cache-aside
- TTL
- Invalidation
- Cache-key design

## Priority 5 — Testing

Demonstrate:

- Unit tests
- Integration tests
- Concurrency tests

## Priority 6 — Observability

Demonstrate:

- Structured logs
- Correlation IDs
- Health checks
- Meaningful errors

## Priority 7 — Deployment

Demonstrate:

- Docker
- Environment configuration
- CI
- Automated tests

---

# 53. What Not to Do

Do not turn this into an unnecessarily complicated microservice architecture.

For this portfolio project, use a **modular monolith**.

```text
                 E-Commerce API
                       │
        ┌──────────────┼──────────────┐
        │              │              │
      Catalog        Orders        Identity
        │              │              │
        └──────────────┼──────────────┘
                       │
                  SQL Server
```

You can demonstrate sophisticated backend engineering without independently deploying:

```text
ProductService
OrderService
InventoryService
PaymentService
NotificationService
...
```

A well-designed modular monolith is a better first serious backend project.

---

# 54. Final Architecture

```text
                         ┌──────────────────────┐
                         │      API Clients     │
                         │ Web / Mobile / Admin │
                         └──────────┬───────────┘
                                    │
                                    ▼
                         ┌──────────────────────┐
                         │   ASP.NET Core 10    │
                         │       Web API        │
                         ├──────────────────────┤
                         │ Auth / Authorization │
                         │ Controllers           │
                         │ Middleware            │
                         │ Validation            │
                         │ Rate Limiting         │
                         └──────────┬───────────┘
                                    │
                                    ▼
                    ┌──────────────────────────────┐
                    │       Application Layer      │
                    ├──────────────────────────────┤
                    │ Catalog                      │
                    │ Cart                         │
                    │ Wishlist                     │
                    │ Checkout                     │
                    │ Orders                       │
                    │ Reviews                      │
                    │ Discounts                    │
                    │ Admin                        │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │         Domain Layer         │
                    ├──────────────────────────────┤
                    │ Entities                     │
                    │ Value Objects                │
                    │ Business Rules               │
                    │ Domain Events                │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │      Infrastructure          │
                    ├──────────────────────────────┤
                    │ EF Core                      │
                    │ SQL Server                   │
                    │ Redis                        │
                    │ File Storage                 │
                    │ Background Jobs              │
                    │ Email                        │
                    └──────────────────────────────┘
```

# 55. Learning Progression

Build the project in increasing levels of difficulty:

```text
CRUD
  ↓
EF Core
  ↓
Authentication
  ↓
Authorization
  ↓
Filtering / Pagination
  ↓
Caching
  ↓
Cart
  ↓
Inventory
  ↓
Transactions
  ↓
Concurrency
  ↓
Idempotency
  ↓
Background Processing
  ↓
Testing
  ↓
Observability
  ↓
Docker
  ↓
CI/CD
```

The finished result should be a **modular, secure, transactional, observable ASP.NET Core 10 backend** rather than a collection of basic CRUD endpoints.
