using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using compendium_discounts;
using System.Text.RegularExpressions;

[assembly: MelonInfo(typeof(CompendiumDiscounts), "Compendium Discounts (Discount every 10 percents)", "1.0.0", "Matthiew Purple but mostly Zephhyr let's be honest")]
[assembly: MelonGame("アトラス", "smt3hd")]

namespace compendium_discounts;
public class CompendiumDiscounts : MelonMod
{
    public static short currentRecord;

    // After replacing localized text
    [HarmonyPatch(typeof(frFont), nameof(frFont.frReplaceLocalizeText))]
    private class Patch
    {
        public static void Postfix(ref string __result)
        {
            // Replace Mido's text to display the correct price when enough macca
            if (__result.Contains("<SP7><FO1>It will cost <CO4>") && __result.Contains("Are you okay with that?"))
            {
                var macca = int.Parse(string.Concat(__result.Replace("<SP7><FO1>It will cost <CO4>", string.Empty).Where(char.IsNumber)));
                macca = macca * Utility.GetDiscountFactor();
                __result = "<SP7><FO1>It will cost <CO4>" + macca + " Macca. <CO0>Are you okay with that?";
            }

            // Replace Mido's text to display the correct price when not enough macca
            else if (__result.Contains("<SP7><FO1>It will cost <CO4>") && __result.Contains("But it seems you don't have enough."))
            {
                var macca = int.Parse(string.Concat(__result.Replace("<SP7><FO1>It will cost <CO4>", string.Empty).Where(char.IsNumber)));
                macca = macca * Utility.GetDiscountFactor();
                __result = "<SP7><FO1>It will cost <CO4>" + macca + " Macca... <CO0>But it seems you don't have enough.";
            }
        }
    }

    // Before and after confirming a summon from compendium
    [HarmonyPatch(typeof(fclEncyc), nameof(fclEncyc.PrepSummon))]
    private class Patch2
    {
        public static void Prefix(ref fclEncyc.readmainwork_tag pwork)
        {
            // Remember the compendium record's ID (for another function later)
            currentRecord = pwork.recindex;
        }

        public static void Postfix(ref fclEncyc.readmainwork_tag pwork, ref int __result)
        {
            // Get the unit about to be summoned
            var pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[pwork.recindex];

            // Get discounted price for that summon
            pwork.mak *= Utility.GetDiscountFactor();

            // If enough macca post-discount but not pre-discount (and stock not full and not already in stock and something idk)
            if (__result == 0 && dds3GlobalWork.DDS3_GBWK.maka >= pwork.mak && datCalc.datCheckStockFull() == 0 && datCalc.datSearchDevilStock(pelem.id) == -1 && pwork.flags == 80)
            {
                pwork.flags = (ushort)(pwork.flags | 1);
                __result = 1;
            }
        }
    }

    // Before starting a script
    [HarmonyPatch(typeof(fclMisc), nameof(fclMisc.fclScriptStart))]
    private class Patch3
    {
        public static void Prefix(ref int StartNo)
        {
            if (StartNo == 18)
            {
                // Get the unit about to be summoned
                var pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[currentRecord];

                // Get the sum of all stats to calculate the summoning price
                var statTotal = fclEncyc.GetDevilParam(pelem, 0) + fclEncyc.GetDevilParam(pelem, 2) + fclEncyc.GetDevilParam(pelem, 3) + fclEncyc.GetDevilParam(pelem, 4) + fclEncyc.GetDevilParam(pelem, 5);
                
                // Sumonning price forumale with applied discount
                var price = (((statTotal * statTotal) / 20) * 100) * Utility.GetDiscountFactor();

                // If enough macca post-discount but not pre-discount (and stock not full and not already in stock and something idk)
                if (dds3GlobalWork.DDS3_GBWK.maka >= price && datCalc.datCheckStockFull() == 0 && datCalc.datSearchDevilStock(pelem.id) == -1)
                {
                    StartNo = 17;
                }
            }
        }
    }

    // Before displaying the prices in the list
    [HarmonyPatch(typeof(CounterCtr), nameof(CounterCtr.Set))]
    private class Patch4
    {
        public static void Prefix(ref int val, ref CounterCtr __instance)
        {
            if (__instance.transform.GetParent().name.Contains("dlistsum_row0"))
            {
                // Apply discount on displayed prices
                val *= Utility.GetDiscountFactor();
            }
        }
    }

    private class Utility
    {
        // Discount calculator
        public static int GetDiscountFactor()
        {
            int compendiumProgress = fclEncyc.fclEncycGetRatio2();

            return compendiumProgress / 10;
        }
    }
}
