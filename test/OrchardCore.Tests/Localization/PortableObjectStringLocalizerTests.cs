using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.Localization;
using OrchardCore.Localization.PortableObject;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class PortableObjectStringLocalizerTests
    {
        private static PluralizationRuleDelegate _csPluralRule = n => ((n == 1) ? 0 : (n >= 2 && n <= 4) ? 1 : 2);
        private static PluralizationRuleDelegate _enPluralRule = n => (n == 1) ? 0 : 1;
        private static PluralizationRuleDelegate _arPluralRule = n => (n == 0 ? 0 : n == 1 ? 1 : n == 2 ? 2 : n % 100 >= 3 && n % 100 <= 10 ? 3 : n % 100 >= 11 ? 4 : 5);
        private Mock<ILocalizationManager> _localizationManager;
        private Mock<ILogger> _logger;

        public PortableObjectStringLocalizerTests()
        {
            _localizationManager = new Mock<ILocalizationManager>();
            _logger = new Mock<ILogger>();
        }

        [Fact]
        public void LocalizerReturnsTranslationsFromProvidedDictionary()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů")
            });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {

                var translation = localizer["ball"];

                Assert.Equal("míč", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsOriginalTextIfTranslationsDoesntExistInProvidedDictionary()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů")
            });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer["car"];

                Assert.Equal("car", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsOriginalTextIfDictionaryIsEmpty()
        {
            SetupDictionary("cs", new CultureDictionaryRecord[] { });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer["car"];

                Assert.Equal("car", translation);
            }
        }

        [Fact]
        public void LocalizerFallbacksToParentCultureIfTranslationDoesntExistInSpecificCulture()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů")
            });
            SetupDictionary("cs-CZ", new[] {
                new CultureDictionaryRecord("car", "auto", "auta", "aut")
            });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs-cz"))
            {
                var translation = localizer["ball"];

                Assert.Equal("míč", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsTranslationFromSpecificCultureIfItExists()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů")
            });
            SetupDictionary("cs-CZ", new[] {
                new CultureDictionaryRecord("ball", "balón", "balóny", "balónů")
            });
            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs-CZ"))
            {
                var translation = localizer["ball"];

                Assert.Equal("balón", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsTranslationWithSpecificContext()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů"),
                new CultureDictionaryRecord("ball", "small",  new [] { "míček", "míčky", "míčků" })
            });

            var localizer = new PortableObjectStringLocalizer("small", _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer["ball"];

                Assert.Equal("míček", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsTranslationWithoutContextIfTranslationWithContextDoesntExist()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů"),
                new CultureDictionaryRecord("ball", "big", new [] { "míček", "míčky", "míčků" })
            });
            var localizer = new PortableObjectStringLocalizer("small", _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer["ball"];

                Assert.Equal("míč", translation);
            }
        }

        [Fact]
        public void LocalizerReturnsFormattedTranslation()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("The page (ID:{0}) was deleted.", "Stránka (ID:{0}) byla smazána.")
            });
            var localizer = new PortableObjectStringLocalizer("small", _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer["The page (ID:{0}) was deleted.", 1];

                Assert.Equal("Stránka (ID:1) byla smazána.", translation);
            }
        }

        [Fact]
        public void HtmlLocalizerDoesNotFormatTwiceIfFormattedTranslationContainsCurlyBraces()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("The page (ID:{0}) was deleted.", "Stránka (ID:{0}) byla smazána.")
            });
            var localizer = new PortableObjectStringLocalizer("small", _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var htmlLocalizer = new PortableObjectHtmlLocalizer(localizer);
                var unformatted = htmlLocalizer["The page (ID:{0}) was deleted.", "{1}"];
                var memStream = new MemoryStream();
                var textWriter = new StreamWriter(memStream);
                var textReader = new StreamReader(memStream);

                unformatted.WriteTo(textWriter, HtmlEncoder.Default);

                textWriter.Flush();
                memStream.Seek(0, SeekOrigin.Begin);
                var formatted = textReader.ReadToEnd();

                textWriter.Dispose();
                textReader.Dispose();
                memStream.Dispose();

                Assert.Equal("Stránka (ID:{1}) byla smazána.", formatted);
            }
        }

        [Theory]
        [InlineData("car", 1)]
        [InlineData("cars", 2)]
        public void LocalizerReturnsOriginalTextForPluralIfTranslationDoesntExist(string expected, int count)
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "míče", "míčů"),
            });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer.Plural(count, "car", "cars");
                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData("zh-Hans", "球", 1, new string[] { "球" })]
        [InlineData("zh-Hans", "球", 2, new string[] { "球" })]
        public void LocalizerReturnsCorrectTranslationForPluralIfNoPluralFormsSpecified(string culture, string expected, int count, string[] translations)
        {
            using (var cultureScope = CultureScope.Create(culture))
            {
                // using DefaultPluralRuleProvider to test it returns correct rule
                TryGetRuleFromDefaultPluralRuleProvider(cultureScope.UICulture, out var rule);
                Assert.NotNull(rule);

                SetupDictionary(culture, new[] { new CultureDictionaryRecord("ball", translations), }, rule);
                var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
                var translation = localizer.Plural(count, "ball", "{0} balls", count);

                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData("míč", 1)]
        [InlineData("2 míče", 2)]
        [InlineData("5 míčů", 5)]
        public void LocalizerReturnsTranslationInCorrectPluralForm(string expected, int count)
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("ball", "míč", "{0} míče", "{0} míčů"),
            });

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("cs"))
            {
                var translation = localizer.Plural(count, "ball", "{0} balls", count);

                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData("míč", 1)]
        [InlineData("2 míče", 2)]
        [InlineData("5 míčů", 5)]
        public void LocalizerReturnsOriginalValuesIfTranslationDoesntExistAndMultiplePluraflFormsAreSpecified(string expected, int count)
        {
            SetupDictionary("en", new CultureDictionaryRecord[] { });
            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("en"))
            {
                var translation = localizer.Plural(count, new[] { "míč", "{0} míče", "{0} míčů" }, count);

                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData("ball", 1)]
        [InlineData("2 balls", 2)]
        public void LocalizerReturnsCorrectPluralFormIfMultiplePluraflFormsAreSpecified(string expected, int count)
        {
            SetupDictionary("en", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("míč", "ball", "{0} balls")
            }, _enPluralRule);
            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, true, _logger.Object);
            using (CultureScope.Create("en"))
            {
                var translation = localizer.Plural(count, new[] { "míč", "{0} míče", "{0} míčů" }, count);

                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData(false, "hello", "hello")]
        [InlineData(true, "hello", "مرحبا")]
        public void LocalizerFallBackToParentCultureIfFallBackToParentUICulturesIsTrue(bool fallBackToParentCulture, string resourceKey, string expected)
        {
            SetupDictionary("ar", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("hello", "مرحبا")
            }, _arPluralRule);
            SetupDictionary("ar-YE", new CultureDictionaryRecord[] { }, _arPluralRule);
            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, fallBackToParentCulture, _logger.Object);
            using (CultureScope.Create("ar-YE"))
            {
                var translation = localizer[resourceKey];

                Assert.Equal(expected, translation);
            }
        }

        [Theory]
        [InlineData(false, new[] { "مدونة", "منتج" })]
        [InlineData(true, new[] { "مدونة", "منتج", "قائمة", "صفحة", "مقالة" })]
        public void LocalizerReturnsGetAllStrings(bool includeParentCultures, string[] expected)
        {
            SetupDictionary("ar", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("Blog", "مدونة"),
                new CultureDictionaryRecord("Menu", "قائمة"),
                new CultureDictionaryRecord("Page", "صفحة"),
                new CultureDictionaryRecord("Article", "مقالة")
            }, _arPluralRule);
            SetupDictionary("ar-YE", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("Blog", "مدونة"),
                new CultureDictionaryRecord("Product", "منتج")
            }, _arPluralRule);

            var localizer = new PortableObjectStringLocalizer(null, _localizationManager.Object, false, _logger.Object);
            using (CultureScope.Create("ar-YE"))
            {
                var translations = localizer.GetAllStrings(includeParentCultures).Select(l => l.Value).ToArray();

                Assert.Equal(expected.Count(), translations.Count());
            }
        }

        [Fact]
        public async Task PortableObjectStringLocalizerShouldRegisterIStringLocalizerOfT()
            => await StartupRunner.Run(typeof(PortableObjectLocalizationStartup), "en", "Hello");

        [Theory]
        [InlineData("ar", 1)]
        [InlineData("ar-YE", 2)]
        [InlineData("zh-Hans-CN", 3)]
        [InlineData("zh-Hant-TW", 3)]
        public void LocalizerWithContextShouldCallGetDictionaryOncePerCulture(string culture, int expectedCalls)
        {
            // Arrange
            SetupDictionary(culture, Array.Empty<CultureDictionaryRecord>());

            var localizer = new PortableObjectStringLocalizer("context", _localizationManager.Object, true, _logger.Object);
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

            // Act
            var translation = localizer["Hello"];

            // Assert
            _localizationManager.Verify(lm => lm.GetDictionary(It.IsAny<CultureInfo>()), Times.Exactly(expectedCalls));
            Assert.Equal("Hello", translation);
        }

        private void SetupDictionary(string cultureName, IEnumerable<CultureDictionaryRecord> records)
        {
            SetupDictionary(cultureName, records, _csPluralRule);
        }

        private void SetupDictionary(string cultureName, IEnumerable<CultureDictionaryRecord> records, PluralizationRuleDelegate pluralRule)
        {
            var dictionary = new CultureDictionary(cultureName, pluralRule);
            dictionary.MergeTranslations(records);

            _localizationManager.Setup(o => o.GetDictionary(It.Is<CultureInfo>(c => c.Name == cultureName))).Returns(dictionary);
        }

        private bool TryGetRuleFromDefaultPluralRuleProvider(CultureInfo culture, out PluralizationRuleDelegate rule)
        {
            return ((IPluralRuleProvider)new DefaultPluralRuleProvider()).TryGetRule(culture, out rule);
        }

        public class PortableObjectLocalizationStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc();
                services.AddPortableObjectLocalization();
            }

            public void Configure(
                IApplicationBuilder app,
                IStringLocalizer<PortableObjectLocalizationStartup> localizer)
            {
                var supportedCultures = new[] { "ar", "en" };
                app.UseRequestLocalization(options =>
                    options
                        .AddSupportedCultures(supportedCultures)
                        .AddSupportedUICultures(supportedCultures)
                        .SetDefaultCulture("en")
                );

                app.Run(async (context) =>
                {
                    await context.Response.WriteAsync(localizer["Hello"]);
                });
            }
        }
    }
}
