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

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void UncheckingRemovesOnlyActiveEntriesAndPreservesCommentsAndInitialDefinitions(string newline)
    {
        var input = StarterConfiguration.Text.Replace("Constructions:",
            "Constructions:\n # Median is available here\n Median\n\tMedian  ").Replace("\n", newline);
        var originalTail = input[input.IndexOf("Initial configuration:", StringComparison.Ordinal)..];
        var updated = ConstructionCatalog.SetEnabled(input, "Median", false);
        Assert.Multiple(() =>
        {
            Assert.That(ConstructionCatalog.GetEnabled(input), Does.Contain("Median"));
            Assert.That(ConstructionCatalog.GetEnabled(input), Does.Not.Contain("Circumcenter"));
            Assert.That(ConstructionCatalog.GetEnabled(updated), Is.EquivalentTo(new[] { "Midpoint", "IntersectionOfLines" }));
            Assert.That(updated, Does.Contain("# Median is available here"));
            Assert.That(updated[updated.IndexOf("Initial configuration:", StringComparison.Ordinal)..], Is.EqualTo(originalTail));
            Assert.That(ConstructionCatalog.SetEnabled(updated, "Median", false), Is.EqualTo(updated));
            Assert.That(updated.Replace(newline, string.Empty), Does.Not.Contain("\r"));
        });
    }

    [Test]
    public void CheckboxChoicesUpdateTheInputAndSurviveFilteringAndReopening()
    {
        var browser = new ConstructionBrowserViewModel(StarterConfiguration.Text);
        var midpoint = browser.Results.Single(option => option.Name == "Midpoint");
        var circumcenter = browser.Results.Single(option => option.Name == "Circumcenter");
        var median = browser.Results.Single(option => option.Name == "Median");
        var updates = new List<string>();
        browser.ConfigurationChanged += updates.Add;
        Assert.That(midpoint.IsIncluded, Is.True);
        Assert.That(median.IsIncluded, Is.False, "An initial definition is not a generation tool selection.");
        circumcenter.IsIncluded = true;
        median.IsIncluded = true;
        midpoint.IsIncluded = false;
        midpoint.IsIncluded = false;
        browser.Query = "circumcenter";
        browser.OutputType = "Circle";
        Assert.That(browser.Results, Is.Empty);
        browser.Query = string.Empty;
        browser.OutputType = "All";
        Assert.Multiple(() =>
        {
            Assert.That(browser.Results.Single(option => option.Name == "Circumcenter"), Is.SameAs(circumcenter));
            Assert.That(circumcenter.IsIncluded, Is.True);
            Assert.That(midpoint.IsIncluded, Is.False);
            Assert.That(browser.EnabledCount, Is.EqualTo("3 enabled for generation"));
            Assert.That(updates, Has.Count.EqualTo(3));
            Assert.That(updates[^1], Is.EqualTo(browser.InputText));
            Assert.That(browser.InputText, Does.Contain("ma = Median(A, B, C)"));
        });
        var reopened = new ConstructionBrowserViewModel(browser.InputText);
        Assert.That(reopened.Results.Where(option => option.IsIncluded).Select(option => option.Name),
            Is.EquivalentTo(new[] { "Circumcenter", "Median", "IntersectionOfLines" }));
    }

    [Test]
    public void EveryCheckboxCanBeToggledInOneSessionWithoutChangingOtherConfigurationSections()
    {
        var browser = new ConstructionBrowserViewModel();
        var originalTail = browser.InputText[browser.InputText.IndexOf("Initial configuration:", StringComparison.Ordinal)..];
        foreach (var option in browser.Results) option.IsIncluded = true;
        Assert.That(ConstructionCatalog.GetEnabled(browser.InputText), Has.Count.EqualTo(ConstructionCatalog.Entries.Count));
        foreach (var option in browser.Results) option.IsIncluded = false;
        Assert.Multiple(() =>
        {
            Assert.That(ConstructionCatalog.GetEnabled(browser.InputText), Is.Empty);
            Assert.That(browser.EnabledCount, Is.EqualTo("0 enabled for generation"));
            Assert.That(browser.InputText[browser.InputText.IndexOf("Initial configuration:", StringComparison.Ordinal)..], Is.EqualTo(originalTail));
        });
    }

    [TestCase("Triangle: A, B, C")]
    [TestCase("Initial configuration:\nTriangle: A, B, C\nConstructions:")]
    public void InvalidInputKeepsTheCatalogReadableAndDisablesEditing(string input)
    {
        var browser = new ConstructionBrowserViewModel(input);
        browser.Results[0].IsIncluded = true;
        Assert.Multiple(() =>
        {
            Assert.That(browser.CanEdit, Is.False);
            Assert.That(browser.Results[0].CanEdit, Is.False);
            Assert.That(browser.Results[0].IsIncluded, Is.False);
            Assert.That(browser.HasSelection, Is.True);
            Assert.That(browser.InputText, Is.EqualTo(input));
            Assert.That(browser.Feedback, Does.Contain("Constructions:"));
        });
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
