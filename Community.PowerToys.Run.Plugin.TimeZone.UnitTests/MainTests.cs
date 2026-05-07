using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.TimeZone.UnitTests;

[TestClass]
public sealed class MainTests
{
    [TestMethod]
    public void Query_returns_conversion_results()
    {
        var main = new Main();

        var results = main.Query(new Query("10:30 AM CST to +10"));

        Assert.IsTrue(results.Count > 0);
        StringAssert.Contains(results[0].Title, "+10:00");
    }

    [TestMethod]
    public void LoadContextMenus_returns_copy_action_for_conversion()
    {
        var main = new Main();
        var result = main.Query(new Query("10:30 AM CST to +10")).First();

        var menus = main.LoadContextMenus(result);

        Assert.AreEqual(1, menus.Count);
    }
}
