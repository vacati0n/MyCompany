# Skill: Performance Engineering

## Purpose
Provide reusable methods to design, measure, and improve runtime performance under real workload conditions.

## Principles
- Optimize based on measured bottlenecks.
- Protect latency and throughput targets explicitly.
- Balance performance gains against maintainability.
- Validate performance under representative load.

## Best Practices
- Define performance objectives before implementation.
- Capture baseline metrics before tuning.
- Profile critical paths to find dominant costs.
- Re-measure after each optimization change.

## Anti-patterns
- Premature optimization without data.
- Single-run benchmarks used as proof.
- Ignoring memory pressure and GC effects.

## Examples
- API target: p95 latency under 200 ms at 500 RPS.
- Measurement set: p50/p95/p99 latency, CPU, memory, saturation, error rate.

## Decision Rules
- Prioritize optimizations with highest user-visible impact.
- Reject optimizations that reduce readability for marginal gains.
- Escalate architecture changes when local tuning cannot meet targets.

## Common Mistakes
- Benchmarking with unrealistic data size.
- Comparing measurements from inconsistent environments.
- Focusing only on average latency.
