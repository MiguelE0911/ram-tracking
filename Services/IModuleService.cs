namespace RamTracking.Services;

using RamTracking.Models;

public interface IModuleService
{
    Task<IReadOnlyList<MemoryModule>> GetModulesAsync();
}