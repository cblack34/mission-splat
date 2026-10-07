using System.Reflection;
using System.Xml.Linq;

namespace MissionSplat.App.Tests;

public class UnityReferenceGuardTests
{
    [Test]
    public void AppProjectFileDoesNotReferenceUnity()
    {
        var offenders = FilesThatShapeTheAppProject()
            .SelectMany(path => UnityReferencesIn(XDocument.Load(path)))
            .ToArray();

        Assert.That(
            offenders,
            Is.Empty,
            "The app project must not reference UnityEngine, UnityEditor, or a Unity package. Found: "
                + string.Join(", ", offenders));
    }

    [Test]
    public void CompiledAppAssemblyDoesNotReferenceUnity()
    {
        var appReference = typeof(UnityReferenceGuardTests).Assembly
            .GetReferencedAssemblies()
            .Single(candidate => candidate.Name == "MissionSplat.App");
        var assembly = Assembly.Load(appReference);

        Assert.That(assembly.GetName().Name, Is.EqualTo("MissionSplat.App"));

        var offenders = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(ReferencesUnity)
            .ToArray();

        Assert.That(
            offenders,
            Is.Empty,
            "The app assembly must not reference UnityEngine, UnityEditor, or a Unity package. Found: "
                + string.Join(", ", offenders));
    }

    private static string FindAppProjectFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "MissionSplat.App", "MissionSplat.App.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "MissionSplat.App.csproj was not found above the test output directory.");
    }

    private static IEnumerable<string> FilesThatShapeTheAppProject()
    {
        var projectPath = FindAppProjectFile();
        yield return projectPath;

        var directory = Directory.GetParent(projectPath);
        while (directory is not null)
        {
            foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets" })
            {
                var candidate = Path.Combine(directory.FullName, name);
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }

            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                yield break;
            }

            directory = directory.Parent;
        }
    }

    private static IEnumerable<string> UnityReferencesIn(XDocument document)
    {
        var root = document.Root;
        if (root is null)
        {
            yield break;
        }

        var sdk = root.Attribute("Sdk")?.Value;
        if (sdk is not null && ReferencesUnity(sdk))
        {
            yield return sdk;
        }

        foreach (var element in root.Descendants())
        {
            if (element.Name.LocalName is "PackageReference" or "Reference" or "ProjectReference" or "FrameworkReference")
            {
                foreach (var attributeName in new[] { "Include", "Update" })
                {
                    var value = element.Attribute(attributeName)?.Value;
                    if (value is not null && ReferencesUnity(value))
                    {
                        yield return value;
                    }
                }
            }

            if (element.Name.LocalName == "HintPath")
            {
                var hintPath = element.Value.Trim();
                if (ReferencesUnity(hintPath))
                {
                    yield return hintPath;
                }
            }
        }
    }

    private static bool ReferencesUnity(string value)
    {
        if (IsUnityName(value))
        {
            return true;
        }

        var segments = value.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (IsUnityName(Path.GetFileNameWithoutExtension(segment.Trim())))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnityName(string name)
    {
        return name.Equals("Unity", StringComparison.OrdinalIgnoreCase)
            || name.Equals("UnityEngine", StringComparison.OrdinalIgnoreCase)
            || name.Equals("UnityEditor", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UnityEditor.", StringComparison.OrdinalIgnoreCase)
            || name.Contains("com.unity", StringComparison.OrdinalIgnoreCase);
    }
}
