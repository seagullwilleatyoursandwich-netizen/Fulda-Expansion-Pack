using System;
using System.Collections.Generic;
using System.Linq;
using GHPC.Equipment.Optics;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using UnityEngine;
using GHPC.State;
using System.Collections;
using MelonLoader;
using GHPC;
using GHPC.Effects.Voices;
using MelonLoader.Utils;
using System.IO;
using NWH.VehiclePhysics;
using HarmonyLib;
using GHPC.Equipment;
using GHPC.Mission;
using GHPC.AI;
using GHPC.Mission.Data;
using ModUtil;
using Rewired;

namespace Fulda1989
{
    public class M60AX : Module
    {
        static MelonPreferences_Entry<bool> m60ax_patch;
        static MelonPreferences_Entry<bool> m60a4_enabled;
        static MelonPreferences_Entry<int> m60a3_chance;

        public static void Config(MelonPreferences_Category category)
        {
            m60ax_patch = category.CreateEntry("M60AX Patch", true);
            m60ax_patch.Comment = "Enables the M60AX/A1";

            m60a4_enabled = category.CreateEntry("M60AX1 Conversion - Adds 120mm M259 cannon", false);

            m60a3_chance = category.CreateEntry("M60A3 Conversion Chance (%)", 45);
            m60a3_chance.Comment = "Random chance for an M60A3 to spawn as an M60AX/A4";
        }
        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;

                GameObject vic_go = vic.gameObject;
                if (!vic.UniqueName.Contains("M60")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                int conversion_chance = 0;

                if (vic.FriendlyName is ("M60A3"))
                {
                    conversion_chance = m60a3_chance.Value;
                }

                vic_go.AddComponent<AlreadyConverted>();

                bool converted_to_m60era = m60ax_patch.Value && UnityEngine.Random.Range(0, 100) < conversion_chance;
                if (converted_to_m60era)
                {
                    vic_go.AddComponent<AlreadyConverted>();

                    if (vic.FriendlyName.Contains("M60A3"))
                    {
                        vic._friendlyName = "M60AX";
                    }

                    Transform hull = vic.transform.Find("M60_meshes/hull_late");
                    Transform turret = null;

                    if (vic.FriendlyName.Contains("M60A3"))
                    {
                        turret = vic.transform.Find("M60A3TTS_rig/hull/turret");
                    }

                    Transform mantlet = null;

                    if (vic.FriendlyName.Contains("M60A3 TTS"))
                    {
                        mantlet = vic.transform.Find("M60A3TTS_rig/hull/turret/main gun mantlet");
                    }


                    GameObject ax_full = GameObject.Instantiate(M60AX_Assets.m60ax_full);

                    Transform hull_armour = M60AX_Assets.m60ax_full.transform.Find("HULL M1/HULL M1 ERA");
                    Transform turret_armour = M60AX_Assets.m60ax_full.transform.Find("TURRET M1/TURRET M1 ERA");
                    Transform mantlet_armour = M60AX_Assets.m60ax_full.transform.Find("MANTLET M1/MANTLET M1 ERA");
                    //foreach (Transform child in hull_m1)
                    //{
                    //    var ua = child.GetComponent<GHPC.UniformArmor>();
                    //    MelonLogger.Msg($"[M60ERA]) hull ERA piece '{child.name}' has UniformArmor: {ua != null}");
                    //}
                    //foreach (Transform child in turret_m1)
                    //{
                    //    var ua = child.GetComponent<GHPC.UniformArmor>();
                    //    MelonLogger.Msg($"[M60ERA]) turret ERA piece '{child.name}' has UniformArmor: {ua != null}");
                    //}
                    //foreach (Transform child in mantlet_m1)
                    //{
                    //    var ua = child.GetComponent<GHPC.UniformArmor>();
                    //    MelonLogger.Msg($"[M60ERA]) mantlet ERA piece '{child.name}' has UniformArmor: {ua != null}");
                    //}

                    // Transform armour_hull = m60ax_full.transform.Find("HULL");
                    // Transform armour_turret = m60ax_full.transform.Find("TURRET");
                    // Transform armour_mantlet = m60ax_full.transform.Find("MANTLET");

                    // HULL
                    if (hull_armour != null)
                    {
                        hull_armour.SetParent(hull, false);

                        hull_armour.localPosition = new Vector3(0.14f, -0.035f, 0.03f);
                        hull_armour.localEulerAngles = new Vector3(0f, 90f, 0f);
                        hull_armour.localScale = new Vector3(0.1725f, 0.1725f, 0.1725f);

                        Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        hull_armour.SetParent(hull_late_follow);
                    }

                    // TURRET
                    if (turret_armour != null)
                    {
                        turret_armour.SetParent(turret, false);

                        turret_armour.localPosition = new Vector3(0.8f, -0.51f, 2.7f);
                        turret_armour.localEulerAngles = new Vector3(0f, 90f, 0f);
                        turret_armour.localScale = new Vector3(1f, 1f, 1f);

                        Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        turret_armour.SetParent(turret_late_follow);
                    }
                    // MANTLET
                    if (mantlet_armour != null)
                    {
                        mantlet_armour.SetParent(mantlet, false);

                        mantlet_armour.localPosition = new Vector3(0.81f, -0.88f, 1.7f);
                        mantlet_armour.localEulerAngles = new Vector3(0f, 90f, 0f);
                        mantlet_armour.localScale = new Vector3(1f, 1f, 1f);

                        Transform mantlet_late_follow = mantlet.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        mantlet_armour.SetParent(mantlet_late_follow);
                    }
                    GameObject.Destroy(ax_full);

                    Transform wheelcosmetic = vic.transform.Find("M60A3TTS_rig/hull/turret/spare_wheel_mantlet");
                    if (wheelcosmetic != null)
                    {
                        wheelcosmetic.gameObject.SetActive(false);
                    }
                    if (vic.FriendlyName.Contains("M60A3"))
                    {
                        Transform camonet1 = vic.transform.Find("M60A3TTS_rig/hull/M60 hull net");
                        if (camonet1 != null)
                        {
                            camonet1.gameObject.SetActive(false);
                        }

                        Transform camonet2 = vic.transform.Find("M60A3TTS_rig/hull/turret/M60 turret net");
                        if (camonet2 != null)
                        {
                            camonet2.gameObject.SetActive(false);
                        }
                        else
                        {
                            Transform camonet3 = vic.transform.Find("--RIG/hull/M60 hull net");
                            if (camonet3 != null)
                            {
                                camonet3.gameObject.SetActive(false);
                            }

                            Transform camonet4 = vic.transform.Find("--RIG/hull/turret/M60 turret net");
                            if (camonet4 != null)
                            {
                                camonet4.gameObject.SetActive(false);
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        public static void Init()
        {
            if (!m60ax_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
