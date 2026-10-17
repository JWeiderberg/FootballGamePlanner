# Architecture decision records (ADR)

Each architecture decision is its own file: `NNNN-short-title.md`, numbered in sequence and never renumbered.

A new or changed ADR is approved by the user through its pull request (checkpoint 2 in [agent-system.md](../agent-system.md)). A superseded decision is not edited; it gets the status "Superseded by NNNN" and a new ADR is written.

## Template

```markdown
# NNNN. Title

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD

## Context
The problem or requirement that forces the decision.

## Decision
What we will do.

## Alternatives
What was considered and why it was rejected.

## Consequences
What becomes easier, what becomes harder, and which GDPR requirements are affected.
```
