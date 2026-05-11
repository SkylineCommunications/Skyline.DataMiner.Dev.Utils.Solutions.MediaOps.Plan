# Hybrid Approach Overview

## Problem
A pure static model is easy to reason about but can be rigid at runtime. A pure dynamic model is flexible but can be harder to validate and maintain.

## Proposal
Use a hybrid model:
- Static contracts for core workflow objects and lifecycle guarantees.
- Dynamic orchestration policies to adapt execution behavior when runtime conditions change.

## Expected Benefits
- Predictable API contracts.
- Better runtime adaptability.
- Safer incremental rollout path.
