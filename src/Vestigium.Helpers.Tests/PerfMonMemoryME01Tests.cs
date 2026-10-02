using System.Diagnostics;
using System.Text.Json;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Memory;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class PerfMonMemoryMe01Tests
{
    [Fact]
    public void ME01_001_catalog_lists_five_objects()
    {
        Assert.Equal(MemoryObjects.All, MemoryCounterCatalog.Categories);
        Assert.True(MemoryCounterCatalog.IsKnownCategory("memory"));
        Assert.True(MemoryCounterCatalog.IsKnownCategory(MemoryObjects.HyperVDynamicMemory));
        Assert.True(MemoryCounterCatalog.IsKnownCategory(MemoryObjects.ReadyBoostCache));
        Assert.False(MemoryCounterCatalog.IsKnownCategory("Paging File"));
        Assert.Throws<ArgumentException>(() => MemoryCounterCatalog.Counters("Paging File"));
    }

    [Fact]
    public void ME01_001_known_counters_cover_memory_object()
    {
        Assert.Contains("Available MBytes", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Committed Bytes", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Commit Limit", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("% Committed Bytes In Use", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Pages/sec", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Copy Read Hits %", MemoryCounterCatalog.Counters(MemoryObjects.Cache));
        Assert.Contains("Bytes cached", MemoryCounterCatalog.Counters(MemoryObjects.ReadyBoostCache));
        Assert.Empty(MemoryCounterCatalog.Counters(MemoryObjects.HyperVDynamicMemory));
    }
