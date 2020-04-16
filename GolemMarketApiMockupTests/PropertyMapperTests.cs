using GolemClientMockAPI.Mappers;
using GolemMarketApiMockup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GolemMarketApiMockupTests
{
    [TestClass]
    public class PropertyMapperTests
    {
        [TestMethod]
        public void MapFromJsonString_ShouldDecodeEmptyProperties()
        {
            var propertyJsonString = "{}";

            var properties = PropertyMappers.MapFromJsonString(propertyJsonString);

            CollectionAssert.AreEquivalent(new Dictionary<string, string>().ToList(),
            properties.ToList());

        }

        [TestMethod]
        public void MapFromJsonString_ShouldDecodeProperties_FromJsonStructure()
        {
            var propertyJsonString = Resources.Property_JsonStructure;

            var properties = PropertyMappers.MapFromJsonString(propertyJsonString).ToList();

            var expected = new Dictionary<string, JToken>()
            {
                { "flatProperty", "someValue" },
                { "golem.srv.comp.container.docker.image", new JArray("golemfactory/ffmpeg") },
                { "golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}", 7 },
                { "golem.srv.comp.container.docker.benchmark{*}", null },
                { "golem.inf.cpu.cores", 4 },
                { "golem.inf.cpu.threads", 8 },
                { "golem.inf.mem.gib", 16 },
                { "golem.inf.storage.gib", 30 },
                { "golem.com.payment.scheme", "after" },
                { "golem.com.pricing.model", "linear" },
                { "golem.com.pricing.est{*}", null },
                { "golem.usage.vector", new JArray("golem.usage.duration_sec") }
            }.ToList();

            CollectionAssert.AreEquivalent(expected.Select(item => item.Key + "|" + item.Value?.ToString()).ToList(),
                properties.Select(item => item.Key + "|" + item.Value?.ToString()).ToList());

        }

        [TestMethod]
        public void MapFromJsonString_ShouldDecodeProperties_FromJsonFlat()
        {
            var propertyJsonString = Resources.Property_JsonFlat;

            var properties = PropertyMappers.MapFromJsonString(propertyJsonString);

            var expected = new Dictionary<string, JToken>()
            {
                { "golem.srv.comp.container.docker.image", new JArray("golemfactory/ffmpeg") },
                { "golem.srv.comp.container.docker.benchmark{golemfactory/ffmpeg}", 7 },
                { "golem.srv.comp.container.docker.benchmark{*}", null },
                { "golem.inf.cpu.cores", 4 },
                { "golem.inf.cpu.threads", 8 },
                { "golem.inf.mem.gib", 16 },
                { "golem.inf.storage.gib", 30 },
                { "golem.com.payment.scheme", "after" },
                { "golem.com.pricing.model", "linear" },
                { "golem.com.pricing.est{*}", null },
                { "golem.usage.vector", new JArray("golem.usage.duration_sec") }
            }.ToList();

            CollectionAssert.AreEquivalent(expected.Select(item => item.Key + "|" + item.Value?.ToString()).ToList(),
                properties.Select(item => item.Key + "|" + item.Value?.ToString()).ToList());
        }


    }
}
