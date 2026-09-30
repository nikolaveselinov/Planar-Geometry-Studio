using GeoGen.Core;
using GeoGen.DesktopApp.Services;
using GeoGen.DesktopApp.ViewModels;
using NUnit.Framework;

namespace GeoGen.DesktopApp.Tests;

public sealed class ConstructionCatalogTests
{
    [Test]
    public void CatalogContainsEveryEngineConstructionWithAnAccurateSignature()
    {
        var engine = Constructions.GetAllConstructions().ToDictionary(item => item.Name);
        Assert.That(ConstructionCatalog.Entries.Select(item => item.Name), Is.EquivalentTo(engine.Keys));
        foreach (var entry in ConstructionCatalog.Entries)
        {
            Assert.Multiple(() =>
            {
                Assert.That(entry.Description, Is.Not.Empty);
                Assert.That(entry.OutputType, Is.EqualTo(engine[entry.Name].OutputType.ToString()));
                Assert.That(entry.ArgumentTypes, Is.EqualTo(string.Join(", ", engine[entry.Name].Signature.ObjectTypes)));
            });
        }
        Assert.That(ConstructionCatalog.Entries.Single(item => item.Name == "ReflectionInLine").Invocation,
            Is.EqualTo("ReflectionInLine(l, A)"));
    }

    [Test]
    public void SearchFindsDescriptionsAndRespectsObjectTypes()
    {
        Assert.That(ConstructionCatalog.Search("CENTER triangle", "Point").Select(item => item.Name),
            Does.Contain("Circumcenter"));
        Assert.That(ConstructionCatalog.Search("circle", "Line").All(item => item.OutputType == "Line"), Is.True);
        var browser = new ConstructionBrowserViewModel { Query = "not-a-construction" };
        Assert.That(browser.HasSelection, Is.False);
        browser.Query = "median";
        Assert.That(browser.Selected?.Name, Is.EqualTo("Median"));
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void EnablingPreservesTheConfigurationAndAvoidsDuplicateEntries(string newline)
    {
        var input = StarterConfiguration.Text.Replace("\n", newline);
        var updated = ConstructionCatalog.Enable(input, "Circumcenter");
        Assert.That(updated, Is.EqualTo(input.Replace("Constructions:", $"Constructions:{newline} Circumcenter")));
        Assert.That(ConstructionCatalog.Enable(updated, "Circumcenter"), Is.EqualTo(updated));
        Assert.That(ConstructionCatalog.Enable(input, "Midpoint"), Is.EqualTo(input));
    }

    [Test]
    public void EnablingRejectsMissingSectionsAndUnknownNames()
    {
        Assert.Throws<ArgumentException>(() => ConstructionCatalog.Enable("Triangle: A, B, C", "Median"));
        Assert.Throws<ArgumentException>(() => ConstructionCatalog.Enable(StarterConfiguration.Text, "NotReal"));
    }

    [Test]
    public void StarterHasAValidConfigurationAndSmallGenerationList()
    {
        var lines = StarterConfiguration.Text.Split('\n').Select(line => line.Trim())
            .Where(line => line.Length != 0 && !line.StartsWith('#')).ToArray();
        var configurationStart = Array.IndexOf(lines, "Initial configuration:");
        var iterationStart = Array.IndexOf(lines, "Iterations: 1");
        var (configuration, _) = Parser.ParseConfiguration(lines[(configurationStart + 1)..iterationStart]);
        Assert.Multiple(() =>
        {
            Assert.That(InputValidator.Validate(StarterConfiguration.Text), Is.Empty);
            Assert.That(configuration.ConstructedObjects, Has.Count.EqualTo(3));
            Assert.That(configurationStart, Is.EqualTo(3));
        });
        foreach (var name in lines[1..configurationStart]) Assert.DoesNotThrow(() => Parser.ParseConstruction(name));
    }
}
