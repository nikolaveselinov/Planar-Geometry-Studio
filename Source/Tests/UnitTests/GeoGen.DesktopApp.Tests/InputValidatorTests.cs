using GeoGen.DesktopApp.Services;
using NUnit.Framework;

namespace GeoGen.DesktopApp.Tests;

public sealed class InputValidatorTests
{
    private const string ValidInput =
        """
        Constructions:

         Median

        Initial configuration:

         Triangle: A, B, C

        Iterations: 1
        MaximalPoints: 1
        MaximalLines: 0
        MaximalCircles: 0
        SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric
        """;

    [Test]
    public void AcceptsCompleteInput()
    {
        Assert.That(InputValidator.Validate(ValidInput), Is.Empty);
    }

    [Test]
    public void RejectsNegativeLimitsAndUnknownSymmetryMode()
    {
        var input = ValidInput
            .Replace("MaximalPoints: 1", "MaximalPoints: -1")
            .Replace("GenerateBothSymmetricAndAsymmetric", "UnknownMode");

        var errors = InputValidator.Validate(input);

        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Some.Contains("MaximalPoints"));
            Assert.That(errors, Has.Some.Contains("UnknownMode"));
        });
    }

    [Test]
    public void RejectsMissingSections()
    {
        var errors = InputValidator.Validate("Iterations: 1");

        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Some.Contains("Constructions"));
            Assert.That(errors, Has.Some.Contains("Initial configuration"));
        });
    }
}
