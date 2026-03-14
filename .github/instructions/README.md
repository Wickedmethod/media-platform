# Nexus Backend Architecture and Development Guidelines

This document consolidates all architectural guidance, development standards, and best practices for the Nexus Smart Home Backend. It provides a comprehensive reference for contributors and AI assistants working within this codebase.

---

## Introduction

The Nexus Smart Home Backend serves as the central nervous system for a privacy-focused, locally-operated smart home platform. It orchestrates communication with devices across multiple protocols, manages automation rules, and exposes a unified API for client applications.

This document establishes the shared understanding necessary for consistent, high-quality contributions to the codebase.

---

## Part One: Architectural Vision

### System Purpose

The platform manages smart home devices operating on Zigbee, WiFi, and Matter protocols without requiring cloud connectivity. All device communication, automation processing, and data storage occur locally, ensuring user privacy and operational reliability regardless of internet availability.

### Architectural Style

The system follows Clean Architecture principles organized around a central Mediator. Business logic resides in the core, isolated from infrastructure concerns. External interactions flow through well-defined boundaries, allowing protocol adapters and persistence mechanisms to change without affecting core behavior.

### Layer Responsibilities

The **Domain Layer** contains the essential business concepts of smart home management. Devices, capabilities, automations, and their relationships exist here as pure representations without technical dependencies. This layer defines what the system is about.

The **Application Layer** orchestrates operations by handling commands and queries through the Mediator. It coordinates workflows between domain entities and infrastructure services, enforcing business rules and maintaining consistency. This layer defines what the system does.

The **Infrastructure Layer** implements technical capabilities required by higher layers. Protocol adapters translate between the unified device model and specific communication requirements. Persistence adapters store and retrieve data. External service integrations live here. This layer defines how the system connects to the outside world.

The **API Layer** provides entry points for external clients through REST endpoints and WebSocket connections. This layer must remain exceptionally thin, serving only to translate HTTP requests into Mediator dispatches and format responses. No business logic belongs here.

---

## Part Two: Communication Patterns

### Mediator-Centric Design

All operations flow through a central Mediator that routes requests to appropriate handlers. Controllers create command or query objects describing what should happen, then dispatch them without knowledge of which handler will respond or how processing will occur.

This indirection provides several benefits: handlers remain focused on single operations, cross-cutting concerns apply consistently through pipeline behaviors, and the system can evolve without changing entry points.

### Command and Query Separation

Operations that change system state use Commands. Creating devices, triggering actions, updating configurations, and deleting records all require commands. Commands may have side effects and typically acknowledge completion without returning substantial data.

Operations that read state use Queries. Listing devices, retrieving automation details, checking current status, and generating reports all use queries. Queries produce no side effects and return the same result when repeated with unchanged underlying data.

Notifications propagate information about events that have occurred. When device state changes, automations trigger, or significant system events happen, notifications inform interested subscribers without requiring direct coupling between event sources and interested parties.

### Event-Driven Propagation

State changes propagate through events rather than direct updates. When a device adapter receives new state from hardware, it emits an event. Interested components subscribe to relevant events: the device registry updates its cache, automations evaluate their triggers, connected clients receive real-time updates.

This decoupling allows components to evolve independently while maintaining system consistency.

---

## Part Three: Domain Model

### Device Concepts

A Device represents any controllable smart home hardware. Each device possesses a unique identifier, belongs to a specific communication protocol, and declares its capabilities. The system never communicates directly with devices; it works with Device entities that adapters translate to protocol-specific operations.

Capabilities describe what devices can do. Binary switching, brightness adjustment, temperature reading, color control, and climate management represent different capability types. The system validates commands against declared capabilities before attempting execution.

The Device Registry serves as the authoritative source for all device information. It maintains the complete list of known devices, their current states, and configuration details. All device operations begin with registry consultation.

### Protocol Abstraction

Device Adapters translate between the unified device model and protocol-specific requirements. Each adapter implements the same interface, allowing the system to interact with devices uniformly regardless of underlying communication mechanisms.

The Zigbee adapter communicates through MQTT topics managed by the Zigbee2MQTT bridge. The WiFi adapter handles direct REST or MQTT communication with devices like Shelly units and ESPHome controllers. The Matter adapter interfaces with the Matter controller for Thread-based and Matter-over-IP devices.

Thread devices deserve special attention: they are never addressed directly by application code. All Thread communication flows through the Matter controller, which manages the Thread radio layer. The application sees only Matter abstractions.

### Automation Framework

Automations connect triggers to actions through optional conditions. When triggers fire and conditions pass, actions execute. This simple model supports complex behaviors through composition.

Triggers respond to various stimuli: device state changes, scheduled times, sensor threshold crossings, or manual activation. Multiple triggers can initiate the same automation.

Conditions gate execution based on current state. Time restrictions, device status checks, and logical combinations allow fine-grained control over when automations should actually run.

Actions define outcomes: device commands, scene activations, notification dispatch, or chaining to other automations. Multiple actions can execute from a single trigger.

---

## Part Four: Foundational Principles

### Single Responsibility

Every component serves exactly one well-defined purpose. Mediator handlers process one command or query type. Protocol adapters handle one communication protocol. Services manage one coherent set of operations. Files contain one class definition.

This focus enables understanding, testing, and modification without unintended consequences.

### Extension Without Modification

New capabilities arrive through new implementations, not changes to existing stable components. Supporting a new protocol means creating a new adapter implementing existing interfaces. Adding new automation triggers means creating new trigger types. Extending capabilities means defining new capability classes.

Existing, tested components remain untouched while the system grows.

### Implementation Substitutability

Any implementation of an interface must work correctly wherever that interface is expected. All device adapters must satisfy their interface contracts completely. Mock implementations for testing must behave consistently with production implementations. This guarantee enables confident refactoring and testing.

### Interface Segregation

Interfaces remain small and focused on specific concerns. Device communication capabilities live in device adapter interfaces. Event publication lives in publisher interfaces. Persistence lives in repository interfaces. No component depends on interface members it does not use.

### Dependency Inversion

High-level business logic depends on abstractions, never on concrete implementations. The application layer defines interfaces that infrastructure implements. Dependency injection wires concrete implementations to abstract dependencies at composition time, not in business logic.

---

## Part Five: Quality Standards

### Pre-Commit Requirements

All code must pass the complete linting suite with all architecture rules satisfied. All tests must pass. The build must complete without errors. Test files must reside in dedicated test directories, not alongside source files. Controllers must contain no business logic. Each file must define at most one class. Path aliases must be used for cross-library imports.

### Architecture Enforcement

The project includes twenty-six custom rules that validate architectural compliance automatically. These rules check layer boundary adherence, controller thickness, CQRS structure, test file organization, and dependency injection patterns. Violations appear as lint errors requiring source code correction.

The rules themselves must never be modified to accommodate non-compliant code. When violations occur, the correct response is always to fix the violating code.

### Testing Expectations

Unit tests verify individual handlers and services in isolation. Dependencies are mocked to focus testing on the component under test. Integration tests verify module interactions and adapter behavior with real or emulated protocols. End-to-end tests verify complete flows through the API.

Test code follows the same quality standards as production code. Mock implementations honor interface contracts. Test utilities receive maintenance attention. Test organization mirrors source organization within dedicated test directories.

---

## Part Six: Documentation Standards

### Purpose Documentation

Every significant component includes documentation explaining its purpose, responsibilities, and relationships. This documentation answers why the component exists and what role it plays in the larger system.

### Behavioral Documentation

Public interfaces include documentation describing expected behavior, parameters, return values, and error conditions. This documentation helps consumers use components correctly without reading implementation details.

### Architectural Decision Records

Significant architectural decisions receive formal documentation explaining the context, decision, consequences, and alternatives considered. These records preserve institutional knowledge and help future contributors understand why the system evolved as it did.

---

## Part Seven: Performance Considerations

### Response Time Expectations

API operations should respond within predictable timeframes. Simple queries should complete quickly. Device command operations should acknowledge promptly, though actual device response may take longer depending on protocol characteristics.

### Resource Efficiency

The system should use resources proportionally to workload. Idle systems should consume minimal resources. Scaling should occur gracefully as device counts and automation complexity increase.

### Event Processing Reliability

Event handlers must process quickly and reliably. Long-running operations should be deferred to avoid blocking event propagation. Failed event handling should not prevent other handlers from receiving events.

---

## Part Eight: Contribution Workflow

### Understanding Before Changing

New contributors should study existing patterns before making changes. The Mediator handlers, device registry, and protocol adapters demonstrate expected approaches. Understanding these patterns ensures consistent contributions.

### Incremental Development

Large features should be developed incrementally with frequent integration. Small, focused changes are easier to review, test, and integrate than large sweeping modifications.

### Quality Gates

All changes must pass automated quality checks before integration. Failing checks indicate problems requiring resolution, not configuration adjustments.

---

## Summary

The Nexus Smart Home Backend maintains high architectural standards through explicit rules, clear layer boundaries, and consistent patterns. Clean Architecture isolates business logic from infrastructure. The Mediator pattern centralizes operation dispatch. CQRS separates state-changing and state-reading operations. Event-driven propagation decouples components while maintaining consistency.

These principles and patterns create a codebase that welcomes contributions, supports testing, and evolves sustainably as the smart home platform grows.

---

_This document consolidates and supersedes individual instruction files previously maintained separately._

_Last updated: 2025-12-02_
