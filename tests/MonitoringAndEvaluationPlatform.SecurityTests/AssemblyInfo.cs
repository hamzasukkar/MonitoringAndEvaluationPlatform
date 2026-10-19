// These tests boot real hosts whose configuration comes from process-wide environment
// variables (Program.cs reads configuration during WebApplication.CreateBuilder, before any
// ConfigureAppConfiguration callback can apply). Two hosts building concurrently with
// different values would read each other's settings, so collections run serially.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
