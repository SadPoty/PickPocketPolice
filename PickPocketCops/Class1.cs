using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.ItemFramework;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using System.Linq;
using Il2CppScheduleOne.PlayerScripts;
using static LawEnforcementEnhancementMod.OfficerSpawnSystem;
using System.Collections.Generic;
using Il2CppFishNet;

[assembly: MelonInfo(typeof(PickPocketCops.PickPocketCops), PickPocketCops.BuildInfo.Name, PickPocketCops.BuildInfo.Version, PickPocketCops.BuildInfo.Author, PickPocketCops.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace PickPocketCops
{
    public static class BuildInfo
    {
        public const string Name = "PickPocketCops";
        public const string Description = "Make cops pickpockable";
        public const string Author = "SadPoty";
        public const string Company = null;
        public const string Version = "1.0.4";
        public const string DownloadLink = null;
    }

    public class PickPocketCops : MelonMod
    {
        // Patch for LawEnforcementEnhancementMod
        System.Type policeManagerType = AccessTools.TypeByName("LawEnforcementEnhancementMod.OfficerSpawnSystem");
        static List<PoliceOfficer> SpawnedOfficer = new List<PoliceOfficer>();
        public static bool IsLEEPatch = false;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                if (policeManagerType != null)
                {
                    var original = AccessTools.Method(policeManagerType, "SpawnOfficer");
                    var postfix = typeof(PickPocketCops).GetMethod(nameof(PostEnablePickPocket), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                    HarmonyInstance.Patch(original, postfix: new HarmonyMethod(postfix));
                    IsLEEPatch = true;
                    MelonLogger.Msg("LawEnforcementEnhancementMod detected !");
                }
                EnablePickpocketOnOfficers();
            }
        }

        public static Il2CppSystem.Collections.Generic.List<PoliceOfficer> GetAllOfficers()
        {
            var officersArray = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>();
            var officersList = new Il2CppSystem.Collections.Generic.List<PoliceOfficer>(officersArray.Length);
            foreach (var officer in officersArray)
                officersList.Add(officer);

            return officersList;
        }

        private static void EnablePickpocketOnOfficers()
        {
            var officersArray = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>();
            var officersList = GetAllOfficers();
            foreach (var officer in officersArray)
            {
                officersList.Add(officer);
            }

            foreach (var officer in officersArray)
            {
                if (officer != null && officer.Inventory != null)
                {
                    officer.Inventory.CanBePickpocketed = true;
                    officer.Inventory.RandomCash = true;

                    SetupCustomLootPool(officer);
                }
            }
            DistributeRandomLootToOfficers(officersList);
            MelonLogger.Msg("PickPocketCops Loaded !");
        }

        private static void SetupCustomLootPool(PoliceOfficer officer)
        {
            var itemDefs = UnityEngine.Resources.FindObjectsOfTypeAll<ItemDefinition>();

            var lootList = new Il2CppSystem.Collections.Generic.List<StorableItemDefinition>();

            StorableItemDefinition GetItem(string name)
            {
                var def = itemDefs.FirstOrDefault(d => d.name == name);
                return def?.TryCast<StorableItemDefinition>();
            }

            void AddItemMultiple(StorableItemDefinition item, int count)
            {
                for (int i = 0; i < count; i++)
                    lootList.Add(item);
            }

            var donut = GetItem("Donut");
            if (donut != null) AddItemMultiple(donut, 6);

            var m1911 = GetItem("M1911");
            if (m1911 != null) AddItemMultiple(m1911, 1);

            var revolver = GetItem("Revolver");
            if (revolver != null) AddItemMultiple(revolver, 1);

            var revolverAmmo = GetItem("RevolverCylinder");
            if (revolverAmmo != null) AddItemMultiple(revolverAmmo, 3);

            var m1911Ammo = GetItem("M1911_Magazine");
            if (m1911Ammo != null) AddItemMultiple(m1911Ammo, 3);

            var newArray = new Il2CppReferenceArray<StorableItemDefinition>(lootList.Count);
            for (int i = 0; i < lootList.Count; i++)
                newArray[i] = lootList[i];

            officer.Inventory.RandomItemDefinitions = newArray;

        }

        public static void DistributeRandomLootToOfficers(Il2CppSystem.Collections.Generic.List<PoliceOfficer> officers)
        {
            if (officers == null || officers.Count == 0)
                return;

            int countToPick = Mathf.Min(Random.Range(7, officers.Count + 1), officers.Count);
            var selectedIndices = new System.Collections.Generic.List<int>();

            while (selectedIndices.Count < countToPick)
            {
                int randomIndex = Random.Range(0, officers.Count);
                if (!selectedIndices.Contains(randomIndex))
                    selectedIndices.Add(randomIndex);
            }

            foreach (var index in selectedIndices)
            {
                var officer = officers[index];
                var itemPool = officer.Inventory.RandomItemDefinitions;

                if (itemPool == null || itemPool.Length == 0)
                    continue;

                var randomItemDef = itemPool[Random.Range(0, itemPool.Length)];
                if (randomItemDef == null)
                    continue;

                int quantity = 1;
                int value;

                switch (randomItemDef.name)
                {
                    case "Revolver":
                    case "RevolverCylinder":
                        value = 6;
                        break;

                    case "M1911":
                    case "M1911_Magazine":
                        value = 7;
                        break;

                    default:
                        value = 1;
                        break;
                }

                var itemInstance = new IntegerItemInstance(randomItemDef, quantity, value);
                officer.Inventory.InsertItem(itemInstance);
            }
        }


        // Patch for LawEnforcementEnhancementMod
        // https://github.com/surrealnirvana/LawEnforcementEnhancementMod
        private static void PostEnablePickPocket(Vector3 position, District district, bool isForDistrictPopulation)
        {
            if (InstanceFinder.NetworkManager.IsClientOnly)
            {
                var Officers = PoliceOfficer.Officers;
                foreach (var officer in Officers)
                {
                    officer.Inventory.CanBePickpocketed = true;
                }
            }
            else
            {
                var coreType = AccessTools.TypeByName("LawEnforcementEnhancementMod.Core");
                var instanceProp = coreType.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var coreInstance = instanceProp.GetValue(null);
                var officerSystemProp = coreType.GetProperty("OfficerSpawnSystem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var officerSystemInstance = officerSystemProp.GetValue(coreInstance);
                var officerSystemType = officerSystemInstance.GetType();
                var activeOfficersField = officerSystemType.GetField("_activeOfficers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var activeOfficersList = activeOfficersField.GetValue(officerSystemInstance) as System.Collections.Generic.List<PoliceOfficer>;

                if (coreType == null)
                {
                    MelonLogger.Error("Core type not found!");
                    return;
                }
                if (instanceProp == null)
                {
                    MelonLogger.Error("Core.Instance property not found!");
                    return;
                }

                if (coreInstance == null)
                {
                    MelonLogger.Error("Core.Instance is null!");
                    return;
                }

                if (officerSystemProp == null)
                {
                    MelonLogger.Error("OfficerSpawnSystem property not found!");
                    return;
                }

                if (officerSystemInstance == null)
                {
                    MelonLogger.Error("OfficerSpawnSystem instance is null!");
                    return;
                }

                if (activeOfficersField == null)
                {
                    MelonLogger.Error("_activeOfficers field not found!");
                    return;
                }

                if (activeOfficersList == null)
                {
                    MelonLogger.Error("_activeOfficers list is null!");
                    return;
                }

                var officer = activeOfficersList.Last();
                SpawnedOfficer.Add(officer);
                officer.Inventory.CanBePickpocketed = true;
                officer.Inventory.RandomCash = true;
                SetupCustomLootPool(officer);
                DistributeRandomLootToOfficer(officer);
            }
        }

        public static List<PoliceOfficer> GetSpawnedOfficers()
        {
            return SpawnedOfficer;
        }

        public static bool GetLEEPatch()
        {
            return IsLEEPatch;
        }

        public static void DistributeRandomLootToOfficer(PoliceOfficer officer)
        {
            var itemPool = officer.Inventory.RandomItemDefinitions;
            var randomItemDef = itemPool[Random.Range(0, itemPool.Length)];

            int quantity = 1;
            int value;

            switch (randomItemDef.name)
            {
                case "Revolver":
                case "RevolverCylinder":
                    value = 6;
                    break;

                case "M1911":
                case "M1911_Magazine":
                    value = 7;
                    break;

                default:
                    value = 1;
                    break;
            }

            var itemInstance = new IntegerItemInstance(randomItemDef, quantity, value);
            officer.Inventory.InsertItem(itemInstance);
        }

    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.PickpocketScreen), "Update")]
    public static class PickpocketScreenUpdatePatch
    {
        private static bool PickFailed = false;
        private static Vector3 oldDestination = new Vector3();

        static void Postfix(Il2CppScheduleOne.UI.PickpocketScreen __instance)
        {
            if (__instance.npc == null)
                return;

            if (!__instance.npc.name.StartsWith("Officer")) // Not Police
                return;

            if (__instance.IsOpen && !PickFailed)
            {
                var Officer = __instance.npc;
                Officer.Movement.MoveSpeedMultiplier = 0;
                oldDestination = Officer.Movement.CurrentDestination;

                PickFailed = true;
            }
            else if (!__instance.IsOpen && PickFailed)
            {
                var Officer = __instance.npc;
                Officer.Movement.MoveSpeedMultiplier = 1;

                if (oldDestination != Vector3.zero)
                {
                    Officer.Movement.SetDestination(oldDestination);
                }
                else
                {
                    Officer.Movement.SetDestination(Officer.transform.position + new Vector3(1, 0, 1));
                }
                PickFailed = false;
            }

            if (__instance.isFail && !__instance.npc.Health.IsKnockedOut && !__instance.npc.Health.IsDead)
            {
                var player = Player.Local;

                if (player != null && !InstanceFinder.NetworkManager.IsClientOnly)
                {
                    var Officer = player.CrimeData.NearestOfficer;
                    player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.NonLethal);
                    player.CrimeData.AddCrime(new Il2CppScheduleOne.Law.Theft(), 2);
                    Officer.BeginFootPursuit_Networked(player.NetworkObject, true);
                    Officer.PursuitBehaviour.SendEnable();
                }
                __instance.isFail = false;
                __instance.OnDestroy();
                __instance.Awake();
            }
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.GameTime.TimeManager), "MarkHostSleepDone")]
    public static class RestockOfficerPatch
    {
        static void Postfix(Il2CppScheduleOne.GameTime.TimeManager __instance)
        {
            var officersList = PickPocketCops.GetAllOfficers();
            PickPocketCops.DistributeRandomLootToOfficers(officersList);

            if (PickPocketCops.GetLEEPatch())
            {
                var SpawnedOfficers = PickPocketCops.GetSpawnedOfficers();
                foreach (var officer in SpawnedOfficers)
                {
                    PickPocketCops.DistributeRandomLootToOfficer(officer);
                }
            }
        }
    }
}
