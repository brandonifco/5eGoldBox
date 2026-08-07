using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.ContentEditor.Models;
using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// Roster CRUD plus the atomic-write invariant (a validation failure leaves
/// the real file untouched), always against a temp copy of the real
/// committed campaign.json, never the committed file itself.
public sealed class CampaignContentServiceTests
{
    [Fact]
    public void SaveRosterMember_EditingAnExistingMemberPersistsTheChange()
    {
        WithTempCampaignFile((service, path) =>
        {
            var model = LoadMember(service, path, "party-member.fighter");
            model.DisplayName = "Renamed Fighter";
            model.SetScore(AbilityV1.Strength, 16);

            var result = service.SaveRosterMember(path, model.ToDefinition());
            Assert.True(result.IsValid, DescribeIssues(result));

            var saved = service.FindRosterMember(path, "party-member.fighter")!;
            Assert.Equal("Renamed Fighter", saved.DisplayName);
            Assert.Equal(16, saved.AbilityScores[AbilityV1.Strength]);
        });
    }

    /// TemporaryHitPoints is a non-nullable int, so "cleared" and "absent"
    /// are the same value -- clearing the Fighter's must remove the property
    /// rather than write a zero.
    [Fact]
    public void SaveRosterMember_ClearingTemporaryHitPointsRemovesThePropertyEntirely()
    {
        WithTempCampaignFile((service, path) =>
        {
            var model = LoadMember(service, path, "party-member.fighter");
            Assert.NotEqual(0, model.TemporaryHitPoints);

            model.TemporaryHitPoints = 0;
            var result = service.SaveRosterMember(path, model.ToDefinition());
            Assert.True(result.IsValid, DescribeIssues(result));

            Assert.DoesNotContain("\"TemporaryHitPoints\"", File.ReadAllText(path));
        });
    }

    [Fact]
    public void SaveRosterMember_DroppingAmmunitionRemovesThePropertyEntirely()
    {
        WithTempCampaignFile((service, path) =>
        {
            var model = LoadMember(service, path, "party-member.rogue");
            Assert.True(model.HasAmmunition);

            model.HasAmmunition = false;
            var result = service.SaveRosterMember(path, model.ToDefinition());
            Assert.True(result.IsValid, DescribeIssues(result));

            Assert.Null(service.FindRosterMember(path, "party-member.rogue")!.Ammunition);

            // The Ranger still carries some, so the property must survive
            // there -- this is a per-member omission, not a file-wide one.
            Assert.NotNull(service.FindRosterMember(path, "party-member.ranger")!.Ammunition);
        });
    }

    [Fact]
    public void SaveRosterMember_PreparedSpellsRoundTripAcrossTheInlineAndExplodedThresholds()
    {
        WithTempCampaignFile((service, path) =>
        {
            // The Wizard's two prepared spells render inline; growing the
            // list past two must switch it to one entry per line, which is
            // the layout rule the byte-identity test proves in the other
            // direction.
            var model = LoadMember(service, path, "party-member.wizard");
            Assert.Equal(2, model.PreparedSpellIds.Count);

            model.PreparedSpellIds.Add("spell.bless");

            var result = service.SaveRosterMember(path, model.ToDefinition());
            Assert.True(result.IsValid, DescribeIssues(result));

            Assert.Equal(
                ["spell.fire-bolt", "spell.magic-missile", "spell.bless"],
                service.FindRosterMember(path, "party-member.wizard")!.PreparedSpellIds);
        });
    }

    /// Documents a real engine gap rather than asserting what one might
    /// assume: CampaignDefinitionValidator does NOT cross-check a roster
    /// entry's RaceId/ClassId/BackgroundId/SelectedSkillIds/EquippedWeaponIds/
    /// PreparedSpellIds against the ruleset -- it checks hit-point sanity,
    /// unique ids, roster size against ActivePartySize, and that ammunition
    /// belongs to an equipped weapon, and nothing else. A campaign naming a
    /// class that does not exist therefore loads clean and only fails later,
    /// during character resolution.
    ///
    /// This is why the roster form uses real dropdowns for those fields
    /// rather than free text: for these ids the picker is the only guard
    /// there is, not a convenience on top of validation. If the validator
    /// ever gains those checks, flip this test to assert rejection.
    [Fact]
    public void SaveRosterMember_AcceptsAnUnknownClassBecauseCampaignValidationDoesNotCrossCheckTheRuleset()
    {
        WithTempCampaignFile((service, path) =>
        {
            var model = LoadMember(service, path, "party-member.fighter");
            model.ClassId = "class.does-not-exist";

            var result = service.SaveRosterMember(path, model.ToDefinition());

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Equal(
                "class.does-not-exist",
                service.FindRosterMember(path, "party-member.fighter")!.ClassId);
        });
    }

    [Fact]
    public void SaveRosterMember_RejectsAmmunitionForAWeaponTheCharacterDoesNotCarryAndLeavesFileUntouched()
    {
        WithTempCampaignFile((service, path) =>
        {
            byte[] before = File.ReadAllBytes(path);

            var model = LoadMember(service, path, "party-member.fighter");
            model.HasAmmunition = true;
            model.AmmunitionWeaponId = "weapon.shortbow";
            model.AmmunitionItemId = "item.arrow";
            model.AmmunitionQuantity = 10;

            var result = service.SaveRosterMember(path, model.ToDefinition());

            Assert.False(result.IsValid);
            Assert.Equal(before, File.ReadAllBytes(path));
        });
    }

    [Fact]
    public void DeleteRosterMember_RemovesAReserveMemberTheActivePartyDoesNotNeed()
    {
        WithTempCampaignFile((service, path) =>
        {
            // The roster carries six for an active party of four, so a
            // reserve member can go without breaking the campaign.
            Assert.True(service.LoadRoster(path).Count > service.LoadActivePartySize(path));

            var result = service.DeleteRosterMember(path, "party-member.barbarian");

            Assert.True(result.IsValid, DescribeIssues(result));
            Assert.Null(service.FindRosterMember(path, "party-member.barbarian"));
        });
    }

    [Fact]
    public void DeleteRosterMember_IsRejectedOnceTheRosterCanNoLongerFieldTheActiveParty()
    {
        WithTempCampaignFile((service, path) =>
        {
            int activePartySize = service.LoadActivePartySize(path);

            // Delete down to exactly the active party size, which is legal...
            foreach (var member in service.LoadRoster(path).Skip(activePartySize).ToList())
            {
                Assert.True(service.DeleteRosterMember(path, member.PartyMemberId).IsValid);
            }

            byte[] before = File.ReadAllBytes(path);

            // ...and one more, which is not.
            var result = service.DeleteRosterMember(
                path,
                service.LoadRoster(path)[0].PartyMemberId);

            Assert.False(result.IsValid);
            Assert.Equal(before, File.ReadAllBytes(path));
        });
    }

    private static CampaignRosterMemberFormModel LoadMember(
        CampaignContentService service,
        string path,
        string partyMemberId)
    {
        return CampaignRosterMemberFormModel.FromDefinition(
            service.FindRosterMember(path, partyMemberId)!);
    }

    private static void WithTempCampaignFile(
        Action<CampaignContentService, string> action)
    {
        string realFilePath = RepositoryLocator.ResolveCampaignPackPaths().Single();
        string tempFile = Path.Combine(
            Path.GetTempPath(),
            $"campaign-service-{Guid.NewGuid():N}.json");

        File.Copy(realFilePath, tempFile);

        try
        {
            action(new CampaignContentService(), tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "; ",
            result.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
    }
}
