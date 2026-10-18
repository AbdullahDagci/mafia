using SusPayi.Domain.Characters;
using SusPayi.Domain.Rules;
using Xunit;
using static SusPayi.Domain.Tests.Fixtures;

namespace SusPayi.Domain.Tests;

public sealed class CrimeTests
{
    [Fact]
    public void Basarili_suc_para_ve_rutbe_puani_verir_ve_bekleme_baslatir()
    {
        var character = NewCharacter();
        var startMoney = character.Money;

        var result = character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(0) { FixedAmount = 750 });

        Assert.True(result.IsSuccess);
        Assert.Equal(CrimeResultKind.Success, result.Value.Result);
        Assert.Equal(750, result.Value.Reward);
        Assert.Equal(startMoney + 750, character.Money);
        Assert.Equal(Pickpocket.RankPoints, character.RankPoints);
        Assert.Equal(Now + DefaultRules.CrimeCooldown, character.CrimeReadyAt);
        Assert.False(character.IsJailed(Now));
    }

    [Fact]
    public void Yakalanan_karakter_hapse_girer_ve_baska_suc_isleyemez()
    {
        var character = NewCharacter();
        var chance = Pickpocket.SuccessChancePercent(character.RankIndex);

        // İlk zar başarısız, ikinci zar kaçışı da kaçırıyor.
        var result = character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(chance, 99));

        Assert.Equal(CrimeResultKind.Caught, result.Value.Result);
        Assert.Equal(Now + Pickpocket.JailTime, character.JailedUntil);

        var later = Now + DefaultRules.CrimeCooldown + TimeSpan.FromSeconds(1);
        var blocked = character.CommitCrime(Pickpocket, DefaultRules, later, new ScriptedRandom(0));
        Assert.False(blocked.IsSuccess);
        Assert.Equal("character.jailed", blocked.Error!.Code);
    }

    [Fact]
    public void Basarisiz_ama_kacan_karakter_hapse_girmez()
    {
        var character = NewCharacter();
        var chance = Pickpocket.SuccessChancePercent(character.RankIndex);

        var result = character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(chance, 0));

        Assert.Equal(CrimeResultKind.Escaped, result.Value.Result);
        Assert.Null(character.JailedUntil);
        Assert.Equal(0, result.Value.Reward);
    }

    [Fact]
    public void Bekleme_suresi_dolmadan_ikinci_suc_reddedilir_ve_tekrar_zamani_doner()
    {
        var character = NewCharacter();
        character.CommitCrime(Pickpocket, DefaultRules, Now, new ScriptedRandom(0));

        var result = character.CommitCrime(Pickpocket, DefaultRules, Now.AddSeconds(30), new ScriptedRandom(0));

        Assert.False(result.IsSuccess);
        Assert.Equal("crime.cooldown", result.Error!.Code);
        Assert.Equal(Now + DefaultRules.CrimeCooldown, result.Error.RetryAt);
    }

    [Fact]
    public void Basari_sansi_alt_ve_ust_sinirlarda_kalir()
    {
        Assert.Equal(CrimeDefinition.MinChancePercent, CrimeCatalog.Find("customs")!.SuccessChancePercent(-10));
        Assert.Equal(CrimeDefinition.MaxChancePercent, Pickpocket.SuccessChancePercent(Ranks.Highest));
    }

    [Fact]
    public void Rutbe_puani_dolunca_terfi_eder_ve_artan_puan_devreder()
    {
        var character = NewCharacter();
        var customs = CrimeCatalog.Find("customs")!;
        var time = Now;
        var promoted = false;

        // Her başarı 2.000 puan; 6 başarıda 12.000 puan birikir: bir terfi, 2.000 artan.
        for (var i = 0; i < 6; i++)
        {
            var result = character.CommitCrime(customs, DefaultRules, time, new ScriptedRandom(0));
            promoted |= result.Value.Promoted;
            time += DefaultRules.CrimeCooldown;
        }

        Assert.True(promoted);
        Assert.Equal(1, character.RankIndex);
        Assert.Equal(2_000, character.RankPoints);
    }

    [Fact]
    public void Katalogdaki_suc_kimlikleri_benzersizdir()
    {
        var ids = CrimeCatalog.All.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
