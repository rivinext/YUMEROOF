using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class BedTriggerTests
{
    [Test]
    public void SleepButton_Disabled_WhenOutsideSleepTime()
    {
        // Setup BedTrigger with required components
        var bedObj = new GameObject();
        var bed = bedObj.AddComponent<BedTrigger>();

        var clockObj = new GameObject();
        var clock = clockObj.AddComponent<GameClock>();
        bed.clock = clock;

        var buttonObj = new GameObject();
        var button = buttonObj.AddComponent<Button>();
        bed.sleepButton = button;

        // Begin within allowed time and open the panel
        clock.currentMinutes = bed.sleepStartMinutes + 60f;
        typeof(BedTrigger).GetMethod("OpenPanel", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(bed, null);

        Assert.IsTrue(button.interactable, "Button should be interactable inside sleep time.");

        // Move time outside of allowed window and update
        clock.currentMinutes = bed.sleepStartMinutes - 60f;
        typeof(BedTrigger).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(bed, null);

        Assert.IsFalse(button.interactable, "Button should be disabled outside sleep time.");
    }
}
