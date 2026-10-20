// <copyright file="MerchantNpcTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.NPC;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for merchant NPCs.
/// </summary>
[TestFixture]
public class MerchantNpcTests
{
    /// <summary>
    /// Tests that each merchant instance uses a separate copy of its configured item storage.
    /// </summary>
    [Test]
    public void MerchantInstancesUseSeparateItemStorages()
    {
        var gameConfiguration = new BasicModel.GameConfiguration();
        var itemDefinition = new BasicModel.ItemDefinition { Width = 1, Height = 1 };
        gameConfiguration.Items.Add(itemDefinition);
        var merchantStore = new BasicModel.ItemStorage();
        merchantStore.Items.Add(
            new BasicModel.Item { Id = Guid.NewGuid(), Definition = itemDefinition, ItemSlot = 0 });
        merchantStore.Items.Add(
            new BasicModel.Item { Id = Guid.NewGuid(), Definition = itemDefinition, ItemSlot = 0 });
        var monsterDefinition = new BasicModel.MonsterDefinition { MerchantStore = merchantStore };

        var firstMerchant = new MerchantNpc(null!, monsterDefinition, null!, gameConfiguration);
        var secondMerchant = new MerchantNpc(null!, monsterDefinition, null!, gameConfiguration);

        Assert.That(firstMerchant.MerchantStorage.ItemStorage, Is.Not.SameAs(merchantStore));
        Assert.That(secondMerchant.MerchantStorage.ItemStorage, Is.Not.SameAs(merchantStore));
        Assert.That(firstMerchant.MerchantStorage.ItemStorage, Is.Not.SameAs(secondMerchant.MerchantStorage.ItemStorage));
        Assert.That(merchantStore.Items.Select(item => item.ItemSlot), Is.All.EqualTo(0));
        Assert.That(firstMerchant.MerchantStorage.Items.Count(), Is.EqualTo(2));
        Assert.That(secondMerchant.MerchantStorage.Items.Count(), Is.EqualTo(2));
    }
}
