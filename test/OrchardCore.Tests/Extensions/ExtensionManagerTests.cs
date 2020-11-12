using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using OrchardCore.DisplayManagement.Events;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Modules;
using OrchardCore.Tests.Stubs;
using Xunit;

namespace OrchardCore.Tests.Extensions
{
    public class ExtensionManagerTests
    {
        private static IHostEnvironment HostingEnvironment
            = new StubHostingEnvironment();

        private static IApplicationContext ApplicationContext
            = new ModularApplicationContext(HostingEnvironment, new List<IModuleNamesProvider>()
            {
                new ModuleNamesProvider()
            });

        private static IFeaturesProvider ModuleFeatureProvider =
            new FeaturesProvider(new[] { new ThemeFeatureBuilderEvents() });

        private static IFeaturesProvider ThemeFeatureProvider =
            new FeaturesProvider(new[] { new ThemeFeatureBuilderEvents() });

        private IExtensionManager ModuleScopedExtensionManager;
        private IExtensionManager ThemeScopedExtensionManager;
        private IExtensionManager ModuleThemeScopedExtensionManager;

        public ExtensionManagerTests()
        {
            ModuleScopedExtensionManager = new ExtensionManager(
                ApplicationContext,
                new[] { new ExtensionDependencyStrategy() },
                new[] { new ExtensionPriorityStrategy() },
                new TypeFeatureProvider(),
                ModuleFeatureProvider,
                new NullLogger<ExtensionManager>()
                );

            ThemeScopedExtensionManager = new ExtensionManager(
                ApplicationContext,
                new[] { new ExtensionDependencyStrategy() },
                new[] { new ExtensionPriorityStrategy() },
                new TypeFeatureProvider(),
                ThemeFeatureProvider,
                new NullLogger<ExtensionManager>()
                );

            ModuleThemeScopedExtensionManager = new ExtensionManager(
                ApplicationContext,
                new IExtensionDependencyStrategy[] { new ExtensionDependencyStrategy(), new ThemeExtensionDependencyStrategy() },
                new[] { new ExtensionPriorityStrategy() },
                new TypeFeatureProvider(),
                ThemeFeatureProvider,
                new NullLogger<ExtensionManager>()
                );
        }

        private class ModuleNamesProvider : IModuleNamesProvider
        {
            private readonly string[] _moduleNames;

            public ModuleNamesProvider()
            {
                _moduleNames = new[]
                {
                    "BaseThemeSample",
                    "BaseThemeSample2",
                    "DerivedThemeSample",
                    "DerivedThemeSample2",
                    "ModuleSample"
                };
            }

            public IEnumerable<string> GetModuleNames()
            {
                return _moduleNames;
            }
        }

        [Fact]
        public void ShouldReturnExtension()
        {
            var extensions = ModuleThemeScopedExtensionManager.GetExtensions()
                .Where(e => e.Manifest.ModuleInfo.Category == "Test");

            Assert.Equal(5, extensions.Count());
        }

        [Fact]
        public void ShouldReturnAllDependenciesIncludingFeatureForAGivenFeatureOrdered()
        {
            var features = ModuleScopedExtensionManager.GetFeatureDependencies("Sample3");

            Assert.Equal(3, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
        }

        [Fact]
        public void ShouldNotReturnFeaturesNotDependentOn()
        {
            var features = ModuleScopedExtensionManager.GetFeatureDependencies("Sample2");

            Assert.Equal(2, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
        }

        [Fact]
        public void GetDependentFeaturesShouldReturnAllFeaturesThatHaveADependencyOnAFeature()
        {
            var features = ModuleScopedExtensionManager.GetDependentFeatures("Sample1");

            Assert.Equal(4, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
            Assert.Equal("Sample4", features.ElementAt(3).Id);
        }

        [Fact]
        public void GetFeaturesShouldReturnAllFeaturesOrderedByDependency()
        {
            var features = ModuleScopedExtensionManager.GetFeatures()
                .Where(f => f.Category == "Test" && !f.Extension.IsTheme());

            Assert.Equal(4, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
            Assert.Equal("Sample4", features.ElementAt(3).Id);
        }

        [Fact]
        public void GetFeaturesWithAIdShouldReturnThatFeatureWithDependenciesOrdered()
        {
            var features = ModuleScopedExtensionManager.GetFeatures(new[] { "Sample2" });

            Assert.Equal(2, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
        }

        [Fact]
        public void GetFeaturesWithAIdShouldReturnThatFeatureWithDependenciesOrderedWithNoDuplicates()
        {
            var features = ModuleScopedExtensionManager.GetFeatures(new[] { "Sample2", "Sample3" });

            Assert.Equal(3, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
        }

        [Fact]
        public void GetFeaturesWithAIdShouldNotReturnFeaturesTheHaveADependencyOutsideOfGraph()
        {
            var features = ModuleScopedExtensionManager.GetFeatures(new[] { "Sample4" });

            Assert.Equal(3, features.Count());
            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample4", features.ElementAt(2).Id);
        }

        /* Theme Base Theme Dependencies */

        [Fact]
        public void GetFeaturesShouldReturnCorrectThemeHeirarchy()
        {
            var features = ThemeScopedExtensionManager.GetFeatures(new[] { "DerivedThemeSample" });

            Assert.Equal(2, features.Count());
            Assert.Equal("BaseThemeSample", features.ElementAt(0).Id);
            Assert.Equal("DerivedThemeSample", features.ElementAt(1).Id);
        }

        /* Theme and Module Dependencies */

        [Fact]
        public void GetFeaturesShouldReturnBothThemesAndModules()
        {
            var features = ModuleThemeScopedExtensionManager.GetFeatures()
                .Where(f => f.Category == "Test");

            Assert.Equal(8, features.Count());
        }

        [Fact]
        public void GetFeaturesShouldReturnThemesAfterModules()
        {
            var features = ModuleThemeScopedExtensionManager.GetFeatures()
                .Where(f => f.Category == "Test");

            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
            Assert.Equal("Sample4", features.ElementAt(3).Id);
            Assert.Equal("BaseThemeSample", features.ElementAt(4).Id);
            Assert.Equal("BaseThemeSample2", features.ElementAt(5).Id);
            Assert.Equal("DerivedThemeSample", features.ElementAt(6).Id);
            Assert.Equal("DerivedThemeSample2", features.ElementAt(7).Id);
        }

        [Fact]
        public void GetFeaturesShouldReturnThemesAfterModulesWhenRequestingBoth()
        {
            var features = ModuleThemeScopedExtensionManager.GetFeatures(new[] { "DerivedThemeSample", "Sample3" });

            Assert.Equal("Sample1", features.ElementAt(0).Id);
            Assert.Equal("Sample2", features.ElementAt(1).Id);
            Assert.Equal("Sample3", features.ElementAt(2).Id);
            Assert.Equal("BaseThemeSample", features.ElementAt(3).Id);
            Assert.Equal("DerivedThemeSample", features.ElementAt(4).Id);
        }

        [Fact]
        public void ShouldReturnNotFoundExtensionInfoWhenNotFound()
        {
            var extension = ModuleThemeScopedExtensionManager.GetExtension("NotFound");

            Assert.False(extension.Exists);
        }
    }
}
