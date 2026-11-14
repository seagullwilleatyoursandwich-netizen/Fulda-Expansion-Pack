using GHPC;
using GHPC.Crew;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fulda1989
{
    // diagnostic printer for debugging cus a ghpc update broke the rh202 post 1.4.3 and now im paranoid lol
    public static class Rh202Diagnostics
    {
        public static void QueueSpawn(Vehicle vic, FireControlSystem rh202Fcs)
        {
            if (!RH202.EnableFcsSpawnDiagnostics || vic == null)
                return;

            LogSpawn(vic, rh202Fcs, "install");
            MelonCoroutines.Start(DelayedSpawnLog(vic, rh202Fcs));
        }

        public static string DescribeHierarchyPath(Transform t, Transform rootStop)
        {
            if (t == null)
                return "(null)";

            List<string> parts = new List<string>();
            Transform current = t;
            while (current != null)
            {
                parts.Add(current.name);
                if (rootStop != null && current == rootStop)
                    break;
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        static IEnumerator DelayedSpawnLog(Vehicle vic, FireControlSystem rh202Fcs)
        {
            yield return null;
            yield return null;
            if (vic != null)
                LogSpawn(vic, rh202Fcs, "install+2f");

            yield return new WaitForSeconds(2f);
            if (vic != null)
                LogSpawn(vic, rh202Fcs, "install+2s");
        }

        static void LogSpawn(Vehicle vic, FireControlSystem rh202Fcs, string phase)
        {
            string unitLabel = vic.FriendlyName ?? vic.name;
            MelonLogger.Msg("[RH202 DIAG][" + phase + "] === " + unitLabel + " ===");

            FireControlSystem[] allFcs = vic.GetComponentsInChildren<FireControlSystem>(true);
            if (rh202Fcs != null)
                LogRh202Anchor(vic, rh202Fcs, phase, allFcs);
            else
                MelonLogger.Warning("[RH202 DIAG][" + phase + "] RH202_FCS reference is null.");

            for (int i = 0; i < allFcs.Length; i++)
            {
                FireControlSystem fcs = allFcs[i];
                if (fcs == null)
                    continue;

                MelonLogger.Msg("[RH202 DIAG][" + phase + "] FCS[" + i + "] go=" + fcs.gameObject.name
                    + " | enabled=" + fcs.enabled + " | active=" + fcs.gameObject.activeInHierarchy
                    + " | stabs=" + fcs.StabsActive + " | stabMode=" + fcs.CurrentStabMode
                    + " | mounts=" + DescribeFcsMounts(fcs)
                    + " | linked=" + DescribeFcsLinkedWeapons(fcs)
                    + " | MainOptic=" + (fcs.MainOptic != null ? fcs.MainOptic.name : "null")
                    + " | NightOptic=" + (fcs.NightOptic != null ? fcs.NightOptic.name : "null")
                    + " | Ref=" + (fcs.ReferenceTransform != null ? fcs.ReferenceTransform.name : "null")
                    + " | CurrentWeaponSystem=" + DescribeFcsCurrentWeapon(fcs));
            }

            WeaponSystemInfo[] weapons = vic._weaponsManager?.Weapons;
            if (weapons == null)
            {
                MelonLogger.Warning("[RH202 DIAG][" + phase + "] weaponsManager.Weapons is null.");
                return;
            }

            int pollutionCount = 0;
            for (int w = 0; w < weapons.Length; w++)
            {
                WeaponSystemInfo info = weapons[w];
                if (info == null)
                    continue;

                WeaponSystem ws = info.Weapon;
                string weaponName = ws != null ? ws.name : "null";
                string infoFcs = info.FCS != null ? info.FCS.gameObject.name : "null";
                string weaponFcs = ws?.FCS != null ? ws.FCS.gameObject.name : "null";
                bool infoRh202 = info.FCS == rh202Fcs;
                bool weaponRh202 = ws?.FCS == rh202Fcs;
                bool split = info.FCS != ws?.FCS;
                bool badPollution = info.Role != WeaponSystemRole.RoofGun && (infoRh202 || weaponRh202);
                if (badPollution)
                    pollutionCount++;

                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Weapon[" + w + "] role=" + info.Role
                    + " | name=" + info.Name + " | weapon=" + weaponName
                    + " | info.FCS=" + infoFcs + " | weapon.FCS=" + weaponFcs
                    + (split ? " | SPLIT" : "")
                    + (badPollution ? " | *** MAIN/COAX ON RH202_FCS ***" : ""));
            }

            ICrewBrain gunner = vic.InfoBroker?._crewManager?.GetCrewBrain(CrewPosition.Gunner);
            WeaponSystemInfo activeInfo = gunner?.WeaponsModule?.ActiveWeapon;
            if (activeInfo != null)
            {
                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Gunner active role=" + activeInfo.Role
                    + " | weapon=" + (activeInfo.Weapon != null ? activeInfo.Weapon.name : "null")
                    + " | weapon.FCS="
                    + (activeInfo.Weapon?.FCS != null ? activeInfo.Weapon.FCS.gameObject.name : "null"));
            }
            else
            {
                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Gunner active weapon: (none / crew not ready)");
            }

            LogMainGunAim(vic, weapons, phase, rh202Fcs);

            if (pollutionCount > 0)
                MelonLogger.Warning("[RH202 DIAG][" + phase + "] " + pollutionCount + " non-roof weapon(s) still point at RH202_FCS.");
        }

        static void LogRh202Anchor(Vehicle vic, FireControlSystem rh202Fcs, string phase, FireControlSystem[] vicFcsScan)
        {
            Transform fcsTransform = rh202Fcs.transform;
            Transform vicRoot = vic != null ? vic.transform : null;
            bool underVic = vicRoot != null && fcsTransform.IsChildOf(vicRoot);
            bool inVicScan = false;
            for (int i = 0; i < vicFcsScan.Length; i++)
            {
                if (vicFcsScan[i] == rh202Fcs)
                {
                    inVicScan = true;
                    break;
                }
            }

            string pathFromVic = DescribeHierarchyPath(fcsTransform, vicRoot);
            string parentName = fcsTransform.parent != null ? fcsTransform.parent.name : "(no parent)";
            Transform parent = fcsTransform.parent;
            string parentPath = parent != null ? DescribeHierarchyPath(parent, vicRoot) : "(no parent)";

            MelonLogger.Msg("[RH202 DIAG][" + phase + "] RH202_FCS anchor"
                + " | under vic.transform=" + underVic
                + " | in GetComponentsInChildren scan=" + inVicScan
                + " | enabled=" + rh202Fcs.enabled
                + " | activeInHierarchy=" + rh202Fcs.gameObject.activeInHierarchy
                + " | stabs=" + rh202Fcs.StabsActive
                + " | stabMode=" + rh202Fcs.CurrentStabMode
                + " | mounts=" + DescribeFcsMounts(rh202Fcs)
                + " | linked=" + DescribeFcsLinkedWeapons(rh202Fcs)
                + " | MainOptic=" + (rh202Fcs.MainOptic != null ? rh202Fcs.MainOptic.name : "null")
                + " | Ref=" + (rh202Fcs.ReferenceTransform != null ? rh202Fcs.ReferenceTransform.name : "null"));

            MelonLogger.Msg("[RH202 DIAG][" + phase + "] RH202_FCS path=" + pathFromVic
                + " | parent=" + parentName
                + " | parentPath=" + parentPath);

            if (!underVic)
                MelonLogger.Warning("[RH202 DIAG][" + phase + "] RH202_FCS is NOT under vic.transform (LateFollow / reparent?).");

            if (!inVicScan)
                MelonLogger.Warning("[RH202 DIAG][" + phase + "] RH202_FCS missing from vic FireControlSystem scan despite reference.");

            WeaponSystem[] linked = rh202Fcs.LinkedWeaponSystems;
            if (linked == null || linked.Length == 0)
                return;

            for (int i = 0; i < linked.Length; i++)
            {
                WeaponSystem roofWeapon = linked[i];
                if (roofWeapon == null)
                    continue;

                Transform roofMuzzle = ResolveMuzzle(roofWeapon);
                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Roof weapon='" + roofWeapon.name + "'"
                    + " | weaponPath=" + DescribeHierarchyPath(roofWeapon.transform, vicRoot)
                    + " | muzzlePath=" + (roofMuzzle != null ? DescribeHierarchyPath(roofMuzzle, vicRoot) : "(null)")
                    + " | sameTransformAsMainMuzzle=" + MuzzleSameAsMainGun(vic, roofMuzzle));
            }
        }

        static bool MuzzleSameAsMainGun(Vehicle vic, Transform roofMuzzle)
        {
            if (vic == null || roofMuzzle == null || vic._weaponsManager?.Weapons == null)
                return false;

            foreach (WeaponSystemInfo info in vic._weaponsManager.Weapons)
            {
                if (info?.Role != WeaponSystemRole.MainGun || info.Weapon == null)
                    continue;

                Transform mainMuzzle = ResolveMuzzle(info.Weapon);
                return mainMuzzle != null && mainMuzzle == roofMuzzle;
            }

            return false;
        }

        static string DescribeFcsMounts(FireControlSystem fcs)
        {
            AimablePlatform[] mounts = fcs?.Mounts;
            if (mounts == null || mounts.Length == 0)
                return "(none)";

            string text = string.Empty;
            for (int i = 0; i < mounts.Length; i++)
            {
                AimablePlatform mount = mounts[i];
                if (mount == null)
                {
                    text += "[null]";
                    continue;
                }

                Transform t = mount.Transform;
                text += "[" + (t != null ? t.name : "?") + " stab=" + mount.StabilizerActive;
                if (t != null)
                    text += " y=" + t.localEulerAngles.y.ToString("F1") + " x=" + t.localEulerAngles.x.ToString("F1");
                text += "]";
            }

            return text;
        }

        static string DescribeFcsLinkedWeapons(FireControlSystem fcs)
        {
            WeaponSystem[] linked = fcs?.LinkedWeaponSystems;
            if (linked == null || linked.Length == 0)
                return "(none)";

            string text = string.Empty;
            for (int i = 0; i < linked.Length; i++)
            {
                WeaponSystem ws = linked[i];
                text += "[" + (ws != null ? ws.name : "null") + "]";
            }

            return text;
        }

        static string DescribeFcsCurrentWeapon(FireControlSystem fcs)
        {
            if (fcs == null)
                return "null";

            try
            {
                WeaponSystem current = fcs.CurrentWeaponSystem;
                return current != null ? current.name : "null";
            }
            catch
            {
                return "(unavailable)";
            }
        }

        static void LogMainGunAim(Vehicle vic, WeaponSystemInfo[] weapons, string phase, FireControlSystem rh202Fcs)
        {
            WeaponSystemInfo mainInfo = null;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i]?.Role == WeaponSystemRole.MainGun)
                {
                    mainInfo = weapons[i];
                    break;
                }
            }

            if (mainInfo?.Weapon == null)
            {
                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Main gun aim: no MainGun entry.");
                return;
            }

            Transform muzzle = ResolveMuzzle(mainInfo.Weapon);
            FireControlSystem mantletFcs = RH202.ResolveMainGunnerFcs(vic, mainInfo, mainInfo.Weapon);
            UsableOptic dayOptic = mantletFcs != null ? Util.GetDayOptic(mantletFcs) : null;
            if (dayOptic == null && mantletFcs?.MainOptic != null)
                dayOptic = mantletFcs.MainOptic;

            if (muzzle == null)
            {
                MelonLogger.Msg("[RH202 DIAG][" + phase + "] Main gun aim: muzzle transform not found.");
                return;
            }

            Vector3 muzzleFwd = muzzle.forward;
            Transform vicRoot = vic != null ? vic.transform : null;
            string line = "[RH202 DIAG][" + phase + "] Main muzzle path=" + DescribeHierarchyPath(muzzle, vicRoot) + " | fwd=" + FormatVector(muzzleFwd);

            if (dayOptic?.Transform != null)
            {
                Vector3 opticFwd = dayOptic.Transform.forward;
                float angle = Vector3.Angle(muzzleFwd, opticFwd);
                line += " | day optic fwd=" + FormatVector(opticFwd) + " | muzzle-vs-optic=" + angle.ToString("F2") + "°";
            }
            else
            {
                line += " | day optic=(not found)";
            }

            MelonLogger.Msg(line);

            WeaponSystemInfo roofInfo = null;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i]?.Role == WeaponSystemRole.RoofGun)
                {
                    roofInfo = weapons[i];
                    break;
                }
            }

            if (roofInfo?.Weapon == null)
                return;

            Transform roofMuzzle = ResolveMuzzle(roofInfo.Weapon);
            if (roofMuzzle == null)
                return;

            Transform mainMuzzle = ResolveMuzzle(mainInfo.Weapon);
            bool sharedMuzzle = mainMuzzle != null && mainMuzzle == roofMuzzle;

            MelonLogger.Msg("[RH202 DIAG][" + phase + "] RCWS muzzle path=" + DescribeHierarchyPath(roofMuzzle, vicRoot)
                + " | fwd=" + FormatVector(roofMuzzle.forward)
                + " | roof weapon.FCS=" + (roofInfo.Weapon.FCS != null ? roofInfo.Weapon.FCS.gameObject.name : "null")
                + " | weaponPath=" + DescribeHierarchyPath(roofInfo.Weapon.transform, vicRoot)
                + (sharedMuzzle ? " | *** SHARED MUZZLE WITH MAIN GUN ***" : ""));
        }

        static Transform ResolveMuzzle(WeaponSystem weapon)
        {
            if (weapon == null)
                return null;

            FieldInfo muzzleIdentityField = typeof(WeaponSystem).GetField("_muzzleIdentity", BindingFlags.Instance | BindingFlags.NonPublic);
            if (muzzleIdentityField != null)
            {
                Transform fromField = muzzleIdentityField.GetValue(weapon) as Transform;
                if (fromField != null)
                    return fromField;
            }

            return weapon.transform.Find("muzzleID");
        }

        static string FormatVector(Vector3 v) => "(" + v.x.ToString("F3") + "," + v.y.ToString("F3") + "," + v.z.ToString("F3") + ")";
    }
}