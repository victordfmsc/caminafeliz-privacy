using NUnit.Framework;

namespace CaminaFeliz.VRBrowser.Tests
{
    /// <summary>
    /// The address bar is the one part of the browser whose behaviour is pure
    /// logic, so it is the one part that can be tested without a headset.
    /// </summary>
    public class UrlUtilityTests
    {
        private const string Template = "https://example.test/?q={0}";

        [TestCase("unity.com", "https://unity.com")]
        [TestCase("www.unity.com/es/products", "https://www.unity.com/es/products")]
        [TestCase("mi-sitio.es", "https://mi-sitio.es")]
        [TestCase("https://a.b/c?d=1", "https://a.b/c?d=1")]
        [TestCase("HTTPS://X.COM", "HTTPS://X.COM")]
        [TestCase("about:blank", "about:blank")]
        public void Resolve_KeepsOrCompletesLocations(string input, string expected) =>
            Assert.AreEqual(expected, UrlUtility.Resolve(input, Template));

        [TestCase("localhost", "http://localhost")]
        [TestCase("localhost:8080/index", "http://localhost:8080/index")]
        [TestCase("127.0.0.1", "http://127.0.0.1")]
        [TestCase("192.168.1.42:3000", "http://192.168.1.42:3000")]
        public void Resolve_TreatsDevServersAsLocations(string input, string expected) =>
            Assert.AreEqual(expected, UrlUtility.Resolve(input, Template));

        [TestCase("unity")]
        [TestCase("cómo funciona webxr")]
        [TestCase("gafas vr.com baratas")]
        [TestCase("-mal.com")]
        public void Resolve_FallsBackToSearch(string input) =>
            Assert.That(UrlUtility.Resolve(input, Template), Does.StartWith("https://example.test/?q="));

        [TestCase("javascript:alert(1)")]
        [TestCase("intent://evil#Intent;end")]
        [TestCase("content://media/external/file")]
        public void Resolve_NeverForwardsUnlistedSchemes(string input) =>
            Assert.That(UrlUtility.Resolve(input, Template), Does.StartWith("https://example.test/?q="));

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Resolve_ReturnsEmptyForBlankInput(string input) =>
            Assert.AreEqual(string.Empty, UrlUtility.Resolve(input, Template));

        [Test]
        public void Search_EncodesTheQuery() =>
            Assert.AreEqual("https://example.test/?q=a%20b%26c", UrlUtility.Search("a b&c", Template));

        [TestCase("https://www.unity.com/es/products", "www.unity.com")]
        [TestCase("not a url", "not a url")]
        public void DisplayName_PrefersTheHost(string url, string expected) =>
            Assert.AreEqual(expected, UrlUtility.DisplayName(url));

        [TestCase("https://a.test/", true)]
        [TestCase("http://a.test/", false)]
        [TestCase("about:blank", false)]
        public void IsSecure_OnlyForHttps(string url, bool expected) =>
            Assert.AreEqual(expected, UrlUtility.IsSecure(url));
    }

    public class BrowserSessionTests
    {
        [Test]
        public void History_TracksBackAndForwardAvailability()
        {
            var session = new BrowserSession();
            Assert.IsFalse(session.CanGoBack);
            Assert.IsFalse(session.CanGoForward);

            session.RecordNavigation("https://a.test/");
            Assert.IsFalse(session.CanGoBack, "a single entry is not a history");

            session.RecordNavigation("https://b.test/");
            Assert.IsTrue(session.CanGoBack);
            Assert.IsFalse(session.CanGoForward);
            Assert.AreEqual("https://b.test/", session.CurrentUrl);
        }

        [Test]
        public void GoingBack_ThenNavigating_TruncatesTheForwardBranch()
        {
            var session = new BrowserSession();
            session.RecordNavigation("https://a.test/");
            session.RecordNavigation("https://b.test/");

            session.NotifyHistoryTraversal(-1);
            session.RecordNavigation("https://a.test/");   // the engine confirms where it landed
            Assert.IsTrue(session.CanGoForward);

            session.RecordNavigation("https://c.test/");
            Assert.IsFalse(session.CanGoForward, "a new navigation drops what was ahead");
            Assert.AreEqual(2, session.History.Count);
        }

        [Test]
        public void RepeatedNavigationToSameUrl_IsNotRecordedTwice()
        {
            var session = new BrowserSession();
            session.RecordNavigation("https://a.test/");
            session.RecordNavigation("https://a.test/");
            Assert.AreEqual(1, session.History.Count);
        }
    }
}
