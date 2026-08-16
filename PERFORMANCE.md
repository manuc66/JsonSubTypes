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

The converter numbers reflect the streamed write path (`Utf8JsonReader`): the write side no longer materializes a `JsonDocument`, which is why the converter's serialization allocations are lower than in earlier revisions. The generator numbers still reflect its `JsonDocument` round-trip (see the README).

Each benchmark class uses a scenario prefix on its method names, so the result rows are unambiguous when the whole suite runs:

- **`Single_`** (`PolymorphismBenchmarks`): a `Cat` declared as its `Animal` base (two `int` properties).
- **`Col_`** (`CollectionBenchmarks`): a list of four mixed animals (`Cat`/`Dog`), the common API payload shape.
- **`Nested_`** (`NestedHierarchyBenchmarks`): a two-level hierarchy (`Payload → Game → Run`), discriminated by two properties.
- **`Pres_`** (`PropertyPresenceBenchmarks`): discrimination by property presence (`KnownSubTypeWithProperty`) instead of a discriminator value.
- **`Leaf_`** (`BaseAsLeafBenchmarks`): serializing/deserializing the polymorphic base type itself, exercising the converter's reflection-based fallback path.
- **`Nw_`** (`NewtonsoftBenchmarks`): the Newtonsoft.Json package through `JsonConvert`.

Filter a scenario with the class name: `dotnet run -c Release --project JsonSubTypes.Benchmarks --filter '*CollectionBenchmarks*'`.

## Machine

The numbers were measured on the machine BenchmarkDotNet reported in that run:

- **CPU**: Intel Core i7-4790 @ 3.60 GHz (Haswell), 8 logical / 4 physical cores
- **OS**: Linux (Manjaro)
- **Runtime**: .NET 10.0.10
- **BenchmarkDotNet**: 0.15.8

Numbers vary across machines and runs; treat them as a relative ordering, not as absolute figures for your hardware.

## Results (JIT, .NET 10)

### Single object

| Benchmark | Converter (`Build()`) | Resolver (`BuildResolver()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: | ---: |
| Serialize | 1.08 µs / 784 B | 0.33 µs / 400 B | 1.18 µs / 656 B |
| Deserialize | 1.51 µs / 424 B | 0.43 µs / 56 B | 0.98 µs / 152 B |

### Collection of 4 objects

| Benchmark | Converter (`Build()`) | Resolver (`BuildResolver()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: | ---: |
| Serialize | 3.20 µs / 2.93 KB | 0.99 µs / 624 B | 4.29 µs / 2600 B |
| Deserialize | 5.53 µs / 1.8 KB | 1.87 µs / 784 B | 4.54 µs / 696 B |

### Nested hierarchy and property presence

| Benchmark | Converter (`Build()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | ---: | ---: |
| Nested deserialize | 1.93 µs / 752 B | 1.15 µs / 144 B |
| Nested serialize | — (no discriminator written) | 1.54 µs / 1016 B |
| Property-presence deserialize | 1.20 µs / 592 B | 1.00 µs / 312 B |
| Property-presence serialize | 0.28 µs / 96 B | 0.27 µs / 96 B |

Nested serialization is measured on the generated engine only: the converter falls back to the plain runtime-type contract when the leaf is registered on an intermediate base (see the README), so its write path does not inject a discriminator there.

## Results (Native AOT)

The generated engine is the only one compatible with Native AOT. The native build is slightly slower than JIT in steady state; its advantage is trimming compatibility and startup time, not throughput.

| Benchmark | JIT | Native AOT |
| :--- | ---: | ---: |
| Single_Generated_Serialize | 1.18 µs / 656 B | 1.43 µs / 640 B |
| Generated_Deserialize (single) | 0.98 µs / 152 B | 1.29 µs / 152 B |

## Newtonsoft.Json comparison

The original `JsonSubTypes` package, through `JsonConvert`. It is a different runtime (reflection-based, no `Utf8JsonWriter`), so these are an order-of-magnitude reference, not a like-for-like benchmark.

| Benchmark | Newtonsoft (`JsonSubTypes`) | STJ Converter (`Build()`) |
| :--- | ---: | ---: |
| Single serialize | 1.41 µs / 2.99 KB | 1.08 µs / 784 B |
| Single deserialize | 2.03 µs / 4.82 KB | 1.51 µs / 424 B |
| Collection serialize (4) | 5.25 µs / 7.22 KB | 3.20 µs / 2.93 KB |
| Collection deserialize (4) | 8.31 µs / 11.21 KB | 5.53 µs / 1.8 KB |

The Newtonsoft package received the same fast-path treatment as the STJ converter: single-level type resolution without the multi-level walk, direct string/int discriminator lookup instead of `ToObject` reflection, and a plain `JValue` discriminator write when no converter applies. Its remaining cost is structural — Newtonsoft loads the payload into a `JObject` and re-deserializes through a `JTokenReader`, a double parse we deliberately kept rather than rewrite the read architecture (date parsing, error paths and `Error` events depend on it).

## Why the engines differ

The ordering is structural, not a tuning artifact:

- The **resolver** is fastest because it delegates to `System.Text.Json` native polymorphism: the runtime routes the type during streaming, with no `JsonDocument` round-trip and no reflection per call.
- The **generated converter** eliminates reflection (compiled routing) but still round-trips the payload through a `JsonDocument` on both the write path (injecting the discriminator) and deserialization, which is why it sits between the resolver and the converter.
- The **converter** streams the write path (`Utf8JsonReader`) and adds runtime type resolution (converter scans, mapping lookups), making it the slowest of the three — and the only engine for hierarchies whose subtypes are only known at runtime.
