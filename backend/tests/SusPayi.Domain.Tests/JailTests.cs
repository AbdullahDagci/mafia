using Xunit;
using static SusPayi.Domain.Tests.Fixtures;

namespace SusPayi.Domain.Tests;

public sealed class JailTests
{
    private static Domain.Characters.Character JailedCharacter(string name)
    {
        var character = NewCharacter(name);
        var chance = Pickpocket.SuccessChancePercent(character.RankIndex);
        character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(chance, 99));
        Assert.True(character.IsJailed(Now.AddSeconds(1)));
        return character;
    }

    [Fact]
    public void Basarili_kurtarma_mahkumu_serbest_birakir_ve_kurtariciyi_odullendirir()
    {
        var buster = NewCharacter("Kurtarici");
        var inmate = JailedCharacter("Mahkum");
        var at = Now.AddSeconds(5);

        var result = buster.Bust(inmate, DefaultRules, at, new ScriptedRandom(0));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Freed);
        Assert.False(inmate.IsJailed(at));
        Assert.Equal(1, buster.Busts);
        Assert.Equal(DefaultRules.BustRankPoints, buster.RankPoints);
        Assert.Equal(at + DefaultRules.BustCooldown, buster.BustReadyAt);
    }

    [Fact]
    public void Basarisiz_kurtarma_kurtariciyi_hapse_atar()
    {
        var buster = NewCharacter("Kurtarici");
        var inmate = JailedCharacter("Mahkum");
        var at = Now.AddSeconds(5);

        var result = buster.Bust(inmate, DefaultRules, at, new ScriptedRandom(99));

        Assert.False(result.Value.Freed);
        Assert.True(inmate.IsJailed(at));
        Assert.Equal(at + DefaultRules.BustFailJailTime, buster.JailedUntil);
    }

    [Fact]
    public void Hapisteki_karakter_baskasini_kurtaramaz()
    {
        var buster = JailedCharacter("Kurtarici");
        var inmate = JailedCharacter("Mahkum");

        var result = buster.Bust(inmate, DefaultRules, Now.AddSeconds(5), new ScriptedRandom(0));

        Assert.Equal("character.jailed", result.Error!.Code);
    }

    [Fact]
    public void Hapiste_olmayan_kisi_kurtarilamaz_ve_kendini_kurtaramaz()
    {
        var buster = NewCharacter("Kurtarici");
        var free = NewCharacter("Serbest");

        Assert.Equal("bust.not_jailed", buster.Bust(free, DefaultRules, Now, new ScriptedRandom(0)).Error!.Code);
        Assert.Equal("bust.self", buster.Bust(buster, DefaultRules, Now, new ScriptedRandom(0)).Error!.Code);
    }

    [Fact]
    public void Parasi_yetmeyen_rusvet_veremez()
    {
        // Başlangıç parası (500) rüşvet bedelinin (5.000) altında.
        var character = JailedCharacter("Rusvetci");
        Assert.True(character.BribeCost(DefaultRules) > character.Money);

        var result = character.BribeOutOfJail(DefaultRules, Now.AddSeconds(1));

        Assert.Equal("bribe.insufficient_funds", result.Error!.Code);
        Assert.True(character.IsJailed(Now.AddSeconds(1)));
    }

    [Fact]
    public void Rusvet_parayi_duser_ve_hapisten_cikarir()
    {
        var character = NewCharacter("Rusvetci");
        character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(0) { FixedAmount = 20_000 });

        var caughtAt = Now + DefaultRules.CrimeCooldown;
        var chance = Pickpocket.SuccessChancePercent(character.RankIndex);
        character.CommitCrime(Pickpocket, DefaultRules, caughtAt, new ScriptedRandom(chance, 99));
        Assert.True(character.IsJailed(caughtAt));

        var moneyBefore = character.Money;
        var cost = character.BribeCost(DefaultRules);
        var result = character.BribeOutOfJail(DefaultRules, caughtAt.AddSeconds(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(cost, result.Value.Cost);
        Assert.Equal(moneyBefore - cost, character.Money);
        Assert.False(character.IsJailed(caughtAt.AddSeconds(1)));
    }

    [Fact]
    public void Hapiste_olmayan_karakter_rusvet_veremez()
    {
        var character = NewCharacter();

        var result = character.BribeOutOfJail(DefaultRules, Now);

        Assert.Equal("bribe.not_jailed", result.Error!.Code);
    }
}
