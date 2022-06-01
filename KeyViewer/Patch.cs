using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ShowBPM
{
    internal static class Patch
    {
        private static float bpm = 0, pitch = 0, playbackSpeed = 1;
        private static bool first = true, beforedt = false;
        private static double beforebpm = 0;

        [HarmonyPatch(typeof(scrCalibrationPlanet),"Start")]
        internal static class scrCalibrationPlanet_Start
        {
            private static void Postfix()
            {
                if (!Main.IsEnabled) return;
                Main.gui.TextObject.SetActive(false);
            }
        }
        


        [HarmonyPatch(typeof(scrUIController), "WipeToBlack")]
        internal static class scrUIController_WipeToBlack_Patch
        {
            private static void Postfix()
            {
                if (!Main.IsEnabled) return;
                Main.gui.TextObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(scnEditor), "ResetScene")]
        internal static class scnEditor_ResetScene_Patch
        {
            private static void Postfix()
            {
                if (!Main.IsEnabled) return;
                Main.gui.TextObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(scrController), "StartLoadingScene")]
        internal static class scrController_StartLoadingScene_Patch
        {
            private static void Postfix()
            {
                if (!Main.IsEnabled) return;
                Main.gui.TextObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(scrController), "Awake")]
        internal static class scrControllerAwake
        {
            public static void Prefix()
            {
                if (!Main.IsEnabled) return;
                Main.language = Main.languages.ContainsKey(RDString.language.ToString()) ? Main.languages[RDString.language.ToString()] : Main.languages["English"];
            }
        }

        [HarmonyPatch(typeof(CustomLevel), "Play")]
        internal static class CustomLevelStart
        {
            private static void Postfix(CustomLevel __instance)
            {
                if (!Main.IsEnabled) return;
                if (!__instance.controller.gameworld) return;
                if (__instance.controller.customLevel == null) return;
                
                LevelStart(__instance.controller);
            }
        }


        [HarmonyPatch(typeof(scrPressToStart), "ShowText")]
        internal static class BossLevelStart
        {
            private static void Postfix(scrPressToStart __instance)
            {
                if (!Main.IsEnabled) return;
                if (!__instance.controller.gameworld) return;
                if (__instance.controller.customLevel != null) return;
                
                LevelStart(__instance.controller);
                
            }

        }

        [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
        internal static class MoveToNextFloor
        {
            public static void Postfix(scrPlanet __instance, scrFloor floor)
            {
                if (!Main.IsEnabled) return;
                if (!__instance.controller.gameworld) return;
                if (floor.nextfloor == null) return;
                List<string> texts = new List<string>();

                bool isTwirl = scrController.instance.isCW;
                double val = scrMisc.GetAngleMoved(floor.entryangle, floor.exitangle, isTwirl) / 3.1415927410125732 *
                             180; // 지금 앵글
                double angle = Math.Round(val);

                double speed = 0, kps = 0, curBPM = 0, nextbpm = 0;
                speed = __instance.controller.speed ;
                curBPM = getRealBPM(floor, bpm) * playbackSpeed * pitch;
                nextbpm = getRealBPM(floor.nextfloor==null? floor:floor.nextfloor, bpm)* playbackSpeed * pitch;
                
                bool isDongta = false;
                if (Main.setting.ignoreMultipress)
                {
                    bool applyMultipressDamage = false;
                    bool flag2 = true;
                    double angleMoved = scrMisc.GetAngleMoved(floor.entryangle, floor.exitangle, !floor.isCCW);
                    double time = scrMisc.AngleToTime(angleMoved,
                        (double) __instance.conductor.bpm * __instance.controller.speed);
                    double num2 = 1.56905098538846;
                    bool flag3 = flag2 & angleMoved > num2;
                    double num3 = (double) __instance.controller.averageFrameTime * 2.5;
                    bool flag4 = flag3 & time > num3;
                    double num4 = 0.0299999993294477;
                    applyMultipressDamage = flag4 & time > num4;

                    isDongta = !applyMultipressDamage && !doubleEqual(nextbpm, curBPM);
                }
                
                


                //curBPM *= isTwirl? (2.0/scrController.instance.planetList.Count):(scrController.instance.planetList.Count*0.5);

                if (Main.setting.onTileBpm)
                    texts.Add(Main.setting.text1.Replace("{value}", format((float) (bpm * speed))));
                if (isDongta || beforedt) curBPM = beforebpm;

                if (Main.setting.onCurBpm) texts.Add(Main.setting.text2.Replace("{value}", format((float) curBPM)));
                if (Main.setting.onRecommandKPS)
                {
                    kps = curBPM / 60;
                    texts.Add(Main.setting.text3.Replace("{value}", Math.Round(kps).ToString()));
                }


                Main.gui.setText(string.Join("\n", texts));

                beforedt = isDongta;
                beforebpm = curBPM;
            }
        }

        [HarmonyPatch(typeof(RDString),"ChangeLanguage")]
        internal static class ChangeLanguage
        {
            public static void Prefix(SystemLanguage language)
            {
                Main.language = Main.languages.ContainsKey(language.ToString()) ? Main.languages[language.ToString()] : Main.languages["English"];
                Main.gui.text.font = RDString.GetFontDataForLanguage(language).font;
            }
        }
        
        [HarmonyPatch(typeof(scrController),"Awake")]
        internal static class Awake
        {
            public static void Prefix()
            {
                if (first)
                {
                    first = false;
                    Main.gui.text.font = RDString.GetFontDataForLanguage(RDString.language).font;
                }
            }
        }

        public static bool doubleEqual(double f1, double f2)
        {
            return Math.Abs(f1 - f2) < 0.0001;
        }

        public static double getRealBPM(scrFloor floor, float bpm)
        {
            /*
            bool isTwirl = floor.isCCW;
            double val = scrMisc.GetAngleMoved(floor.entryangle,floor.exitangle,!isTwirl)/3.1415927410125732*180; // 지금 앵글
            double angle = Math.Round(val);

            double speed = 0, kps = 0, curBPM = 0;
            speed = floor.controller.speed;
            if (angle == 0) angle = 360;
            curBPM = (180/angle) * (speed * bpm);
            return curBPM;*/
            if (floor == null)
                return bpm;
            if (floor.nextfloor == null)
                return floor.controller.speed * bpm;
            return 60.0/(floor.nextfloor.entryTime - floor.entryTime);

        }
        
        public static string Repeat(string value, int count)
        {
            return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
        }

        public static string format(float v)
        {
            return string.Format("{0:0." + Repeat(Main.setting.zero? "0":"#", Main.setting.showDecimal) + "}", v);
        }

        public static void LevelStart(scrController __instance)
        {
            Main.gui.TextObject.SetActive(true);

            List<string> texts = new List<string>();
            float kps = 0;

            
            if (__instance.customLevel!=null)
            {
                
                pitch = (float)__instance.customLevel.levelData.pitch/100;
                var before = typeof(GCS).GetField("currentSpeedRun",AccessTools.all);
                var after = typeof(GCS).GetField("currentSpeedTrial",AccessTools.all);

                if (GCS.standaloneLevelMode) pitch *= (float)(before==null? 
                    (after==null? 1.0f:after.GetValue(null))  : before.GetValue(null));
                playbackSpeed = scnEditor.instance.playbackSpeed;
                
                bpm = __instance.customLevel.levelData.bpm * playbackSpeed * pitch;
            }
            else
            {
                pitch = __instance.conductor.song.pitch;
                //if (!GCS.currentSpeedRun.Equals(1)) pitch *= GCS.currentSpeedRun;
                playbackSpeed = 1;
                bpm = __instance.conductor.bpm * pitch;
            }

            float cur = bpm;
            if (__instance.currentSeqID!=0)
            {
                double speed = __instance.controller.speed;
                cur = (float)(bpm * speed);
            }


            if (Main.setting.onTileBpm)
                texts.Add(Main.setting.text1.Replace("{value}", format(cur)));
            if (Main.setting.onCurBpm) texts.Add(Main.setting.text2.Replace("{value}", format(cur)));

            if (Main.setting.onRecommandKPS)
            {
                kps = cur / 60;
                texts.Add(Main.setting.text3.Replace("{value}", Math.Round(kps).ToString()));
            }

            Main.gui.setText(string.Join("\n", texts));
            Main.gui.setSize(Main.setting.size);
        }

    }
}
