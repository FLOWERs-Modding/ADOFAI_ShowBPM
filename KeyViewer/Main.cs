using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;
using UnityEngine;
using GDMiniJSON;


namespace ShowBPM
{
    public static class Main
    {
        public static bool IsEnabled { get; private set; }

        internal static TextBehaviour gui;
        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }

        public static Harmony harmony;
        public static Setting setting;

        public static Dictionary<string, Language> languages = new Dictionary<string, Language>()
        {
            {"Korean", new Korean()},
            {"English", new English()}
        };

        public static Language language = new English();



        internal static void Setup(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            setting = new Setting();
            setting = UnityModManager.ModSettings.Load<Setting>(modEntry);
            modEntry.OnToggle = OnToggle;
            /*
            gui.text.rectTransform.anchoredPosition = new Vector2(setting.x*Screen.width, setting.y*Screen.height*-1);
            gui.text.fontSize = setting.size;
           */ 


        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if(value)
            {
                Start(modEntry);
                gui = new GameObject().AddComponent<TextBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(gui);
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                gui.TextObject.SetActive(false);
                
            } else
            {
                gui.TextObject.SetActive(false);
                UnityEngine.Object.DestroyImmediate(gui);
                gui = null;
                Stop(modEntry);
            }
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            
            
            setting.onTileBpm = GUILayout.Toggle(setting.onTileBpm, language.showTileBPM);
            if (setting.onTileBpm)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                setting.text1 =  MoreGUILayout.NamedTextField(language.setTileBPM, setting.text1, 300f);
                GUILayout.EndHorizontal();
            }

            setting.onCurBpm = GUILayout.Toggle(setting.onCurBpm, language.showRealBPM);
            if (setting.onCurBpm)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                setting.text2 = MoreGUILayout.NamedTextField(language.setRealBPM, setting.text2, 300f);
                GUILayout.EndHorizontal();
            }
            setting.onRecommandKPS = GUILayout.Toggle(setting.onRecommandKPS, language.showKPS);
            
            if (setting.onRecommandKPS)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                setting.text3 = MoreGUILayout.NamedTextField(language.setKPS, setting.text3, 300f);
                GUILayout.EndHorizontal();
            }
            

            if (setting.onTileBpm || setting.onCurBpm || setting.onRecommandKPS)
            {
                GUILayout.Label("   ");
                setting.useShadow = GUILayout.Toggle(setting.useShadow, language.setShadow);
                gui.shadowText.enabled = setting.useShadow;
            
                setting.useBold = GUILayout.Toggle(setting.useBold, language.setBold);
                gui.text.fontStyle = setting.useBold ? FontStyle.Bold : FontStyle.Normal;
                
                setting.zero = GUILayout.Toggle(setting.zero, language.setZeroPlaceHolder);

                setting.ignoreMultipress = GUILayout.Toggle(setting.ignoreMultipress, language.ignoreMultipress);

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                int newV =
                    (int)MoreGUILayout.NamedSlider(
                        language.showDecimal,
                        setting.showDecimal,
                        0,
                        6,
                        300f,
                        roundNearest: 1f,
                        valueFormat: "{0:0.##}");
                if (newV != setting.showDecimal)
                {
                    setting.showDecimal = newV;
                }
                
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                float newX =
                    MoreGUILayout.NamedSlider(
                        language.setX,
                        setting.x,
                        -0.1f,
                        1.1f,
                        300f,
                        roundNearest: 0.01f,
                        valueFormat: "{0:0.##}");
                if (newX != setting.x)
                {
                    setting.x = newX;
                    gui.setPosition(setting.x,setting.y);
                }
                GUILayout.EndHorizontal();
                
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                float newY =
                    MoreGUILayout.NamedSlider(
                        language.setY,
                        setting.y,
                        -0.1f,
                        1.1f,
                        300f,
                        roundNearest: 0.01f,
                        valueFormat: "{0:0.##}");

                if (newY != setting.y)
                {
                    setting.y = newY;
                    gui.setPosition(setting.x, setting.y);
                }

                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                float newSize =
                    MoreGUILayout.NamedSlider(
                        language.setSize,
                        setting.size,
                        1f,
                        100f,
                        300f,
                        roundNearest: 1f,
                        valueFormat: "{0:0.##}");


                if ((int) newSize != setting.size)
                {
                    setting.size = (int) newSize;
                    gui.setSize(setting.size);
                }

                GUILayout.EndHorizontal();
                
                string[] aligns = new string[] {language.alignLeft, language.alignCenter, language.alignRight };
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(language.setAlign);

                GUIStyle guiStyle = new GUIStyle(GUI.skin.button);
                
                foreach (string text in aligns)
                {
                    if (setting.align == Array.IndexOf(aligns, text)) guiStyle.fontStyle = FontStyle.Bold;
                    if(GUILayout.Button(text,guiStyle)) setting.align = Array.IndexOf(aligns, text);
                    guiStyle.fontStyle = FontStyle.Normal;
                }
                

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                gui.text.alignment = gui.toAlign(setting.align);
            }

        }
       
        

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            setting.Save(modEntry);
        }

        private static void Start(UnityModManager.ModEntry modEntry)
        {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }

        private static void Stop(UnityModManager.ModEntry modEntry)
        {
            harmony.UnpatchAll(modEntry.Info.Id);
        }
    }
}
