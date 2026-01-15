# SuperEcomManager

A large-scale, multi-tenant, subscription-based SaaS platform for eCommerce sellers to manage their entire eCommerce operations from a unified portal.

## Product Vision

- **One unified portal** to manage entire eCommerce operations
- **Multi-channel support** - Shopify, Amazon, Flipkart, Meesho, WooCommerce, and custom websites
- **Unified shipping** - Default Shiprocket integration with support for custom courier APIs
- **Strong NDR handling** - Comprehensive Non-Delivery Report management
- **Mobile-first UI** - Fully responsive and future-ready for dedicated mobile apps
- **Enterprise-ready** - Sellable SaaS product with multi-tenant architecture

## Technology Stack

### Frontend
- **Framework:** Next.js 14+ (App Router)
- **Language:** TypeScript
- **Styling:** Tailwind CSS
- **State Management:** Zustand + React Query
- **UI Components:** Radix UI / shadcn/ui

### Backend
- **Framework:** .NET 10 Web API
- **Architecture:** Clean Architecture
- **Database:** PostgreSQL
- **Cache:** Redis
- **Message Queue:** RabbitMQ
- **Background Jobs:** Hangfire
- **Containerization:** Docker

## Documentation

| Document | Description |
|----------|-------------|
| [System Architecture](docs/01-system-architecture.md) | High-level system design and architecture |
| [Database Schema](docs/02-database-schema.md) | Complete database design with all tables |
| [Backend Structure](docs/03-backend-structure.md) | .NET project structure and organization |
| [Frontend Structure](docs/04-frontend-structure.md) | Next.js project structure and routing |
| [Feature Flags & RBAC](docs/05-feature-flags-rbac.md) | Feature management and access control |
| [Multi-Platform Integration](docs/06-multi-platform-integration.md) | Channel adapter pattern and integrations |
| [NDR Workflow](docs/07-ndr-workflow.md) | NDR management system design |
| [API Design](docs/08-api-design.md) | REST API specifications and contracts |
| [Mobile Readiness](docs/09-mobile-readiness.md) | Mobile app strategy and API design |
| [Development Roadmap](docs/10-development-roadmap.md) | Phase-wise development plan |
| [Future Enhancements](docs/11-future-enhancements.md) | AI, automation, and future features |

## Core Features

### Multi-Tenant Architecture
- Schema-per-tenant data isolation
- Tenant-specific configurations
- Super Admin platform management

### Subscription Management
- Feature-based subscription plans
- Tenant-level feature overrides
- Usage tracking and limits

### Sales Channel Integration
- Shopify (Initial)
- Amazon SP-API
- Flipkart Seller API
- Meesho Partner API
- WooCommerce REST API
- Custom website integration

### Order Management
- Unified order model across all platforms
- Real-time order sync via webhooks
- Order lifecycle management
- Bulk order operations

### Shipment Management
- Shiprocket integration (default)
- Custom courier API support
- AWB generation and tracking
- Shipment lifecycle (OFD, NDR, RTO)

### NDR Management
- NDR inbox dashboard
- Employee assignment and tracking
- Multi-channel follow-ups (Call, WhatsApp, SMS, Email)
- Reattempt scheduling
- NDR analytics and reporting

### Inventory Management
- Centralized inventory
- Auto-sync across platforms
- Stock movement tracking
- Low stock alerts

### Finance & Analytics
- Platform-wise P&L reports
- Expense management
- Revenue tracking
- Performance analytics

## Quick Start

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL 15+
- Redis 7+
- Docker (optional)

### Development Setup

```bash
# Clone the repository
git clone https://github.com/your-org/superecommanager.git
cd superecommanager

# Backend setup
cd src/SuperEcomManager.API
dotnet restore
dotnet run

# Frontend setup (new terminal)
cd frontend
npm install
npm run dev
```

### Docker Setup

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f
```

## Project Structure

```
superecommanager/
├── docs/                          # Documentation
├── src/                           # Backend source code
│   ├── SuperEcomManager.Domain/
│   ├── SuperEcomManager.Application/
│   ├── SuperEcomManager.Infrastructure/
│   ├── SuperEcomManager.Integrations/
│   └── SuperEcomManager.API/
├── frontend/                      # Frontend source code
├── tests/                         # Test projects
├── docker/                        # Docker configurations
└── scripts/                       # Utility scripts
```

## Contributing

Please read our contributing guidelines before submitting pull requests.

## License

Proprietary - All rights reserved.
