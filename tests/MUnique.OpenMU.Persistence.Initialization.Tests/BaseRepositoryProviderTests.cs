// <copyright file="BaseRepositoryProviderTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using System.Threading;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.InMemory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;
using Moq;

/// <summary>
/// Tests for the <see cref="BaseRepositoryProvider"/>.
/// </summary>
[TestFixture]
public class BaseRepositoryProviderTests
{
    /// <summary>
    /// Tests that concurrent first accesses initialize the provider only once.
    /// Previously, each of them ran the initialization, which failed when registering
    /// the same repository again, e.g. when players logged in at the same time.
    /// </summary>
    [Test]
    public async Task ConcurrentFirstAccessesInitializeOnceAsync()
    {
        var provider = new SlowlyInitializingRepositoryProvider();
        using var start = new ManualResetEventSlim();
        var accesses = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return provider.GetRepository(typeof(string));
            }))
            .ToList();

        start.Set();
        var repositories = await Task.WhenAll(accesses).ConfigureAwait(false);

        Assert.That(provider.InitializationCount, Is.EqualTo(1));
        Assert.That(repositories, Has.All.Not.Null);
    }

    /// <summary>
    /// Tests that concurrent first accesses create one in-memory repository for an entity.
    /// </summary>
    [Test]
    public async Task ConcurrentFirstAccessesCreateOneMemoryRepositoryAsync()
    {
        var provider = new InMemoryRepositoryProvider();
        using var start = new ManualResetEventSlim();
        var accesses = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return provider.GetRepository<CastleSiegeGuildRegistration>();
            }))
            .ToList();

        start.Set();
        var repositories = await Task.WhenAll(accesses).ConfigureAwait(false);

        Assert.That(repositories, Has.All.Not.Null);
        var registration = new BasicModel.CastleSiegeGuildRegistration { Id = Guid.NewGuid() };
        ((IMemoryRepository)repositories[0]).Add(registration.Id, registration);
        Assert.That(
            await repositories[^1].GetByIdAsync(registration.Id).ConfigureAwait(false),
            Is.SameAs(registration));
    }

    private sealed class SlowlyInitializingRepositoryProvider : BaseRepositoryProvider
    {
        private int _initializationCount;

        public int InitializationCount => this._initializationCount;

        protected override void Initialize()
        {
            Interlocked.Increment(ref this._initializationCount);

            // Gives the other accesses the time to arrive while the initialization is running.
            Thread.Sleep(100);
            this.RegisterRepository(typeof(string), new Mock<IRepository>().Object);
            base.Initialize();
        }
    }
}
