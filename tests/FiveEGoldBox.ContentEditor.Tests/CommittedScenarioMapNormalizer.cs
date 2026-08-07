using FiveEGoldBox.ContentEditor.Models;
using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// Rewrites every committed scenario's ExplorationMap through the editor's
/// own renderer, normalizing each floor's property order to the one canonical
/// order ScenarioJsonFormatting writes.
///
/// Skipped by default, matching this repo's existing writer convention
/// (FixtureWriter, ContentPackSchemaWriter, WriteFrozenCombatTranscripts):
/// un-skip, run, re-skip. Unlike those, this one should rarely be needed --
/// it exists because hand-authored content drifted into two different
/// orderings (Hollow Mill wrote Npcs before Doors; Watchtower the reverse),
/// which meant a no-op save of one file was byte-identical and of the other
/// was not. Run it if that ever happens again rather than hand-moving JSON
/// blocks, since the renderer's output is the definition of "canonical" here.
public sealed class CommittedScenarioMapNormalizer
{
    [Fact(Skip = "Writer -- un-skip to normalize committed scenario map ordering in place.")]
    public void NormalizeCommittedScenarioMaps()
    {
        ScenarioContentService service = new();

        foreach (string path in RepositoryLocator.ResolveScenarioPackPaths())
        {
            foreach (var location in service.LoadLocations(path))
            {
                var map = service.FindExplorationMap(path, location.LocationId);

                if (map is null)
                {
                    continue;
                }

                var result = service.SaveExplorationMap(
                    path,
                    location.LocationId,
                    ExplorationMapFormModel.FromDefinition(map).ToDefinition());

                Assert.True(result.IsValid);
            }
        }
    }
}
