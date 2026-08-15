# Performance

This document is the methodology and the full results behind the [performance summary in the README](./#performance-measured). It covers how the benchmarks are set up, the machine they ran on, and how to reproduce them.

## How to reproduce

The benchmarks live in the `JsonSubTypes.Benchmarks` project. Run the whole suite:

```bash
dotnet run -c Release --project JsonSubTypes.Benchmarks
```

Or filter to a scenario class:

```bash
dotnet run -c Release --project JsonSubTypes.Benchmarks --filter '*PolymorphismBenchmarks*'
```

The command runs every benchmark twice: once under the JIT (`DefaultJob`) and once as a native binary (`NativeAOT` job). The reflection-based engines (converter, resolver, Newtonsoft) report `NA` under the NativeAOT job: they need reflection, which the native host disables.

### What is measured

Each scenario is a micro-benchmark of serializing/deserializing a small object graph, declared as its polymorphic base type. The numbers below are **mean** values from a single representative run, with allocations per operation.

The scenarios:

- **Single object**: a `Cat` declared as its `Animal` base (two `int` properties).
- **Collection**: a list of four mixed animals (`Cat`/`Dog`), the common API payload shape.
- **Nested hierarchy**: a two-level hierarchy (`Payload → Game → Run`), discriminated by two properties.
- **Property presence**: discrimination by property presence (`KnownSubTypeWithProperty`) instead of a discriminator value.

## Machine

The numbers were measured on the machine BenchmarkDotNet reported in that run:

- **CPU**: Intel Core i7-4790 @ 3.60 GHz (Haswell), 8 logical / 4 physical cores
- **OS**: Linux (Manjaro)
- **Runtime**: .NET 10.0.9
- **BenchmarkDotNet**: 0.15.8

Numbers vary across machines and runs; treat them as a relative ordering, not as absolute figures for your hardware.

## Results (JIT, .NET 10)

### Single object

| Benchmark | Converter (`Build()`) | Resolver (`BuildResolver()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: | ---: |
| Serialize | 1.14 µs / 856 B | 0.33 µs / 400 B | 1.18 µs / 656 B |
| Deserialize | 1.50 µs / 648 B | 0.43 µs / 56 B | 0.98 µs / 152 B |

### Collection of 4 objects

| Benchmark | Converter (`Build()`) | Resolver (`BuildResolver()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: | ---: |
| Serialize | 4.16 µs / 3288 B | 0.99 µs / 624 B | 4.29 µs / 2600 B |
| Deserialize | 5.64 µs / 2744 B | 1.87 µs / 784 B | 4.54 µs / 696 B |

### Nested hierarchy and property presence

| Benchmark | Converter (`Build()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: |
| Nested deserialize | 1.73 µs / 1152 B | 1.15 µs / 144 B |
| Nested serialize | — (no discriminator written) | 1.54 µs / 1016 B |
| Property-presence deserialize | 1.21 µs / 776 B | 1.00 µs / 312 B |
| Property-presence serialize | 0.27 µs / 96 B | 0.27 µs / 96 B |

Nested serialization is measured on the generated engine only: the converter falls back to the plain runtime-type contract when the leaf is registered on an intermediate base (see the README), so its write path does not inject a discriminator there.

## Results (Native AOT)

The generated engine is the only one compatible with Native AOT. The native build is slightly slower than JIT in steady state; its advantage is trimming compatibility and startup time, not throughput.

| Benchmark | JIT | Native AOT |
| :--- | ---: | ---: |
| Generated_Serialize (single) | 1.18 µs / 656 B | 1.43 µs / 640 B |
| Generated_Deserialize (single) | 0.98 µs / 152 B | 1.29 µs / 152 B |

## Newtonsoft.Json comparison

The original `JsonSubTypes` package, through `JsonConvert`. It is a different runtime (reflection-based, no `Utf8JsonWriter`), so these are an order-of-magnitude reference, not a like-for-like benchmark.

| Benchmark | Newtonsoft (`JsonSubTypes`) | STJ Converter (`Build()`) |
| :--- | ---: | ---: |
| Single serialize | 1.54 µs / 3.07 KB | 1.14 µs / 856 B |
| Single deserialize | 2.54 µs / 5.26 KB | 1.50 µs / 648 B |
| Collection serialize (4) | 5.51 µs / 7.53 KB | 4.16 µs / 3288 B |
| Collection deserialize (4) | 10.21 µs / 12.96 KB | 5.64 µs / 2744 B |

## Why the engines differ

The ordering is structural, not a tuning artifact:

- The **resolver** is fastest because it delegates to `System.Text.Json` native polymorphism: the runtime routes the type during streaming, with no `JsonDocument` round-trip and no reflection per call.
- The **generated converter** eliminates reflection (compiled routing) but still round-trips the payload through a `JsonDocument` to inject the discriminator on write and to read it on deserialize, which is why it sits between the resolver and the converter.
- The **converter** keeps the same `JsonDocument` round-trip and adds runtime type resolution (converter scans, mapping lookups), making it the slowest of the three — and the only engine for hierarchies whose subtypes are only known at runtime.
