# Instrument Blotter - Working Notes

## 1. Context & Problem Space

You are designing a next-generation Instrument Blotter for a fixed-income trading desk (bonds).

### Key Characteristics

- Traders need one place to view and interact with all instruments
- The blotter is a large, grid-based UI:
  - **Rows** = instruments
  - **Columns** = heterogeneous data (market, pricing, analytics, controls)
- Column data comes from many different internal systems
- These systems are:
  - Owned by different teams
  - Deployed independently
  - Operationally independent (can fail independently)

### The Core Challenge

> How to design a blotter that composes data from many owners, without tight coupling, while remaining performant, resilient, and extensible.

---

## 2. Foundational Architectural Principles

Throughout the discussion, several principles emerged and stayed consistent:

### 2.1 Decoupling

- The frontend must not be hardcoded to backend systems
- Backend ownership boundaries must be respected
- APIs are the only coupling point

### 2.2 Independent Ownership & Deployment

- Each logical group of columns is owned by one team
- Teams should be able to:
  - Deploy independently
  - Change internal implementations freely
  - Add new columns without frontend changes

### 2.3 Failure Isolation

- A failure in one column set must not:
  - Break other columns
  - Take down the entire blotter
- Partial degradation is acceptable and expected

---

## 3. Frontend Vision

Web-based blotter UI, likely:

- Angular
- High-performance grid (e.g. AG Grid)

### Frontend Responsibilities

- Discover available data providers
- Discover columns dynamically
- Render columns using metadata
- Manage subscriptions
- Handle user interaction

### Key Principle

> The frontend is a composition layer, not a business logic layer.

**No:**

- Hardcoded column definitions
- Hardwired backend dependencies
- Provider-specific logic

---

## 4. Data Provider Registry

A central structural decision.

### Purpose

- Acts as a **directory**, not a data service
- Lives at a well-known URL
- Enables runtime discovery

### What It Provides

- List of available data providers
- Metadata (name, description)
- Endpoints for connecting to providers

### What It Deliberately Does Not Do

- No data aggregation
- No proxying
- No runtime dependency on provider availability

### Benefits

- New providers can be added without UI redeployments
- Providers can evolve independently
- Loose coupling between UI and backend ecosystem

---

## 5. Data Provider Model

Each Data Provider is:

- An independently deployable service
- Owned by a single team
- Responsible for a logical set of columns

### Provider Responsibilities

| Responsibility | Description |
|----------------|-------------|
| Define column metadata | Expose column definitions via API |
| Source and transform data | Fetch from downstream systems |
| Stream updates | Push incremental changes to subscribers |
| Handle write-back | Process user edits (if applicable) |
| Act as adapter | Insulate UI from downstream complexity |

### Technology Stance

- No mandated language or framework
- Java, .NET, etc. all acceptable
- **API contract is the stability boundary**

---

## 6. Column Discovery & Metadata

Each provider exposes a **Column Definition API** that returns:

| Field | Description |
|-------|-------------|
| Column IDs | Unique identifiers |
| Display names | Human-readable labels |
| Categories / groupings | Logical organization |
| Data types | String, number, date, etc. |
| Formatting hints | Decimals, currency, etc. |
| Editability flags | Read-only vs editable |
| Allowed values / enums | Validation constraints |
| Configuration schema | Optional customization |

### Benefits

- Fully dynamic column rendering
- Context-sensitive configuration panels
- Minimal frontend logic
- Runtime extensibility

> Goal: A complete description of "everything the provider supports".

---

## 7. Subscription Model (Read Path)

The default interaction model is **push-based streaming**.

### Subscription Semantics

- UI subscribes to:
  - A set of instruments
  - A set of columns
- Providers push incremental updates

### Runtime Flexibility

- Columns can be added/removed dynamically
- Instruments can be added/removed dynamically
- Subscriptions evolve without reconnecting

### Default Characteristics

- Read-only
- High-frequency
- Stateless from the UI's perspective

---

## 8. Bidirectional Columns (Write Path)

Most columns are read-only, but a small, critical subset are **bidirectional**.

### Examples

- Pricing model selection
- Spread / price adjustment

### Design Intent

Write-back is:

- Explicit
- Column-specific
- Not the default

### Interaction Model

```
1. User edits a cell
2. UI sends a simple SetValue request
3. Provider:
   - Validates input
   - Translates intent
   - Publishes to downstream systems
4. Provider returns acknowledgement or rejection
5. UI reflects the result visually
```

> Providers act as adapters, insulating the UI from downstream complexity.

---

## 9. Communication Technology

Not locked down early, but constraints identified:

### Requirements

- High throughput
- Low latency
- Streaming
- Bidirectional communication
- Language neutrality

### gRPC as Candidate

Strong candidate because:

- Streaming support
- Efficient binary serialization
- Cross-language
- Clean API contracts

> Architecture remains **transport-agnostic**.

---

## 10. Performance & Latency Considerations

Key assumptions acknowledged:

- Very high update volumes
- Trading-grade latency expectations
- Grid virtualisation is essential
- Incremental (cell-level) updates are mandatory
- Partial provider failure is normal

### Architectural Support

- Supports isolation
- Avoids centralized bottlenecks
- Allows providers to scale independently

---

## 11. Pros & Cons

### Pros

| Benefit | Description |
|---------|-------------|
| Strong modularity | Clear boundaries between components |
| Independent ownership | Teams control their own destiny |
| Runtime extensibility | Add providers without redeployment |
| Failure isolation | Partial degradation, not total failure |
| Technology freedom | No mandated stack |

### Cons / Trade-offs

| Trade-off | Mitigation |
|-----------|------------|
| More moving parts | Good documentation and contracts |
| Operational complexity | Investment in observability |
| API governance required | Disciplined versioning strategy |
| Observability needs | Centralized logging and monitoring |

> Trade-offs seen as appropriate for the problem domain.

---

## 12. Documentation Direction

### Explicitly Not Wanted

- Full architecture review board document
- Heavy governance framing
- Security/compliance deep dives (yet)

### Goal

> A technical design overview suitable for introducing the design to technical stakeholders.

---

## 13. Deliverables Produced

### 13.1 Technical Design Overview

A complete, structured document containing:

- Motivation & goals
- System overview
- Frontend & provider responsibilities
- Column metadata model
- Read/write interaction patterns
- Performance considerations
- Trade-offs and open questions

### 13.2 Diagrams (Mermaid)

- Logical component diagram (protocol-agnostic)
- Subscription & write-back sequence diagram
- Column lifecycle state diagram

### 13.3 Exported Artifact

- `instrument-blotter-technical-design.md`
- Ready for GitHub/GitLab, VS Code, sharing, iteration

---

## 14. Current State Summary

### What Exists

- A clear architectural shape
- A shared mental model
- A technical document suitable for circulation
- Explicit boundaries between:
  - UI
  - Registry
  - Providers
  - Downstream systems

### Ready To

- Socialise the design
- Gather feedback
- Prototype provider APIs
- Iterate without re-litigating fundamentals

---

## 15. Natural Next Steps

Not required now, but obvious future evolutions:

- [ ] Provider API sketch (interfaces only)
- [ ] Example provider implementation
- [ ] Observability expectations
- [ ] Schema evolution/versioning strategy
- [ ] Formal architecture review pack (if needed)
