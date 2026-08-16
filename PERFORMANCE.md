# Performance

This document is the methodology and the full results behind the [performance summary in the README](./#performance-measured). It covers how the benchmarks are set up, the machine they ran on, and how to reproduce them.

## How to reproduce

Prerequisites:

- A .NET 10 SDK (`dotnet --version` >= 10.0.100).
- A native compiler for the `NativeAOT` job: `clang` on Linux, the "Desktop development with C++" workload on Windows, or the Xcode Command Line Tools on macOS. The first run compiles the whole benchmark host as a native binary, which takes a few minutes.

The benchmarks live in the `JsonSubTypes.Benchmarks` project. Run the whole suite:

```bash
dotnet run -c Release --project JsonSubTypes.Benchmarks -- --filter "*"
```

Or filter to a scenario class:

```bash
dotnet run -c Release --project JsonSubTypes.Benchmarks -- --filter '*PolymorphismBenchmarks*'
```

The command runs every benchmark twice: once under the JIT (`DefaultJob`) and once as a native binary (`NativeAOT` job). The reflection-based engines (converter, resolver, Newtonsoft) cannot run under the NativeAOT job: the native host disables reflection, and their benchmark methods throw `NotSupportedException`, so BenchmarkDotNet reports them as `NA`/failed there and the Native AOT table below only reports the generated engine.

## Caveats

- These are **micro-benchmarks**: they measure a narrow scenario in isolation (a small object graph declared as its polymorphic base type), not real application throughput. Use them as a relative ordering between the engines, not as a prediction of end-to-end performance.
- The numbers are specific to the machine and configuration below. **Always measure on your own hardware** before making a decision.
- Times vary between runs and across runtime/OS/BenchmarkDotNet versions; a few percent of run-to-run variance is normal (the machine here is a 2014 desktop CPU). Allocations are stable and are the more reliable figure.
- Each reported value is the **mean** of a single BenchmarkDotNet run (warm-up + multiple iterations, outliers removed), as shown in the [sample run](#sample-run-verified) below.
- Never compare times across machines. Comparing allocations across machines is meaningful.

## Machine

The numbers below and in the sample run were measured on the same machine, which BenchmarkDotNet reports as:

- **CPU**: Intel Core i7-4790 @ 3.60 GHz (Haswell), 8 logical / 4 physical cores
- **OS**: Linux (Manjaro)
- **Runtime**: .NET 10.0.10 (SDK 10.0.110)
- **BenchmarkDotNet**: 0.15.8

The results were originally measured on .NET 10.0.9 and re-verified on .NET 10.0.10 ([sample run](#sample-run-verified)); the numbers reproduced within the run-to-run noise.

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

## Sample run (verified)

Verbatim output of the reproduction command above (`--filter "*"`) on the documented machine, .NET 10.0.10 / BenchmarkDotNet 0.15.8, run on 2026-08-16. The single-object summary is shown; the collection, nested-hierarchy, property-presence and Newtonsoft summaries reproduced the corresponding tables within the same run-to-run noise. The reflection-based rows under `NativeAOT` are the expected `NA` (see [How to reproduce](#how-to-reproduce)).

```
BenchmarkDotNet v0.15.8, Linux Manjaro Linux
Intel Core i7-4790 CPU 3.60GHz (Max: 0.80GHz) (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.110
  [Host]     : .NET 10.0.10 (10.0.10, 42.42.42.42424), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.10 (10.0.10, 42.42.42.42424), X64 RyuJIT x86-64-v3
  NativeAOT  : .NET 10.0.10, X64 NativeAOT x86-64-v3

| Method                | Job        | Toolchain         | Mean       | Error    | StdDev   | Gen0   | Allocated |
|---------------------- |----------- |------------------ |-----------:|---------:|---------:|-------:|----------:|
| Generated_Serialize   | DefaultJob | Default           | 1,136.9 ns | 22.51 ns | 24.09 ns | 0.1564 |     656 B |
| Resolver_Serialize    | DefaultJob | Default           |   336.5 ns |  5.12 ns |  4.79 ns | 0.0954 |     400 B |
| Converter_Serialize   | DefaultJob | Default           | 1,175.4 ns | 23.17 ns | 33.23 ns | 0.2041 |     856 B |
| Converter_Deserialize | DefaultJob | Default           | 1,486.4 ns | 20.95 ns | 17.50 ns | 0.1545 |     648 B |
| Resolver_Deserialize  | DefaultJob | Default           |   397.2 ns |  4.48 ns |  3.97 ns | 0.0134 |      56 B |
| Generated_Deserialize | DefaultJob | Default           | 1,020.0 ns | 20.33 ns | 23.41 ns | 0.0362 |     152 B |
| Generated_Serialize   | NativeAOT  | Latest ILCompiler | 1,477.5 ns | 27.75 ns | 27.25 ns | 0.1526 |     640 B |
| Resolver_Serialize    | NativeAOT  | Latest ILCompiler |         NA |       NA |       NA |     NA |        NA |
| Converter_Serialize   | NativeAOT  | Latest ILCompiler |         NA |       NA |       NA |     NA |        NA |
| Converter_Deserialize | NativeAOT  | Latest ILCompiler |         NA |       NA |       NA |     NA |        NA |
| Resolver_Deserialize  | NativeAOT  | Latest ILCompiler |         NA |       NA |       NA |     NA |        NA |
| Generated_Deserialize | NativeAOT  | Latest ILCompiler | 1,251.0 ns | 24.31 ns | 29.85 ns | 0.0362 |     152 B |
```
