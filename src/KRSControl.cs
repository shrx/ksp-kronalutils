using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    /**
     * KSPAddon that allows to bind different input controls.
     * <para> For now this only works with <see cref="KRSHinge"/>s. </para>
     * <para> It is commented out because it causes some problems when placing struts. </para>
     */
    //   [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KRSControler: MonoBehaviour
    {
        private GUIStyle style = new GUIStyle(HighLogic.Skin.window);
        private static Rect windowSizeMinimized = new Rect(Screen.width - 250, Screen.height - 50, 150, 50);
        private static Rect windowSizeMaximized = new Rect(Screen.width - 400, Screen.height - 510, 300, 500);
        private Rect windowSize = windowSizeMaximized;
        private bool isSetting = false;
        private bool isClearing = false;
        private KeyValuePair<KRSHinge, string> currentControl;
        private bool currentReversed;
        private Vector2 scrollPos = Vector2.zero;
        private bool isReversing;

        private IEnumerable<KeyValuePair<string, KRSInputAttribute>> GetInputFieldsAttributes(KRSHinge c)
        {
            foreach (var a in c.GetType().GetFields())
            {
                foreach (var b in (KRSInputAttribute[])a.GetCustomAttributes(typeof(KRSInputAttribute), true))
                {
                    yield return new KeyValuePair<string, KRSInputAttribute>(a.Name, b);
                }
            }
        }

        private string GetInputFieldValue(KRSHinge c, string fieldname)
        {
            return (string)c.GetType().GetField(fieldname).GetValue(c);
        }

        private void SetInputFieldValue(KRSHinge c, string fieldname, string value)
        {
            c.GetType().GetField(fieldname).SetValue(c, value);
        }

        private bool IsOnEditor()
        {
            return (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH);
        }


        public void OnGUI()
        {
            if (!IsOnEditor()) return;
            this.windowSize = GUILayout.Window(GetInstanceID(), this.windowSize, WindowGUI, "KRS Control", style);
        }

        public void WindowGUI(int id)
        {
            GUI.DragWindow(new Rect(0f, 0f, 1000f, 50f));
            GUILayout.BeginVertical("box");
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
            foreach (var c in (KRSHinge[])UnityEngine.Object.FindObjectsOfType(typeof(KRSHinge)))
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + c.part.partInfo.title + "</b>");
                foreach (var f in GetInputFieldsAttributes(c)) {
                    string inputName;
                    bool inputIsAxis, inputIsReversed;
                    var fieldValue = GetInputFieldValue(c, f.Key);
                    var active = KRSInputAttribute.GetInputDef(fieldValue, out inputName, out inputIsAxis, out inputIsReversed);
                    var isSettingCurrent = isSetting && this.currentControl.Value == f.Key;
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label(f.Value.guiName + ": ");
                    GUILayout.Label(!isSettingCurrent ? (active ? inputName : "<i><none></i>") : "<i><press key / axis></i>");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal("box");
                    GUI.enabled = !(isSetting || active);
                    if (GUILayout.Button("Set"))
                    {
                        this.currentControl = new KeyValuePair<KRSHinge, string>(c, f.Key);
                        this.isSetting = true;
                    }
                    GUI.enabled = isSettingCurrent || active;
                    if (GUILayout.Button("Clear"))
                    {
                        this.currentControl = new KeyValuePair<KRSHinge, string>(c, f.Key);
                        this.isClearing = true;
                    }
                    var r = GUILayout.Toggle(inputIsReversed, "Reversed");
                    if (r != inputIsReversed)
                    {
                        this.currentControl = new KeyValuePair<KRSHinge, string>(c, f.Key);
                        this.currentReversed = r;
                        this.isReversing = true;
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public void Update()
        {
            if (!IsOnEditor()) return;

            if (this.isSetting)
            {
                foreach (var key in KRSUtils.BindableKeys)
                {
                    if (Input.GetKey(key))
                    {
                        SetInputFieldValue(this.currentControl.Key, this.currentControl.Value,
                            KRSInputAttribute.GetInputString(key.ToString(), false, this.currentReversed));
                        this.isSetting = false;
                    }
                }

                for (int i = 0; i < Input.GetJoystickNames().Length; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var axis = String.Format(KRSUtils.AxisFormat, i, j);
                        if (Math.Abs(Input.GetAxis(axis)) > 0.25f)
                        {
                            SetInputFieldValue(this.currentControl.Key, this.currentControl.Value,
                                KRSInputAttribute.GetInputString(axis, true, this.currentReversed));
                            this.isSetting = false;
                        }
                    }
                }
            }
            else if (isClearing)
            {
                this.isClearing = false;
                SetInputFieldValue(this.currentControl.Key, this.currentControl.Value, "");
            }
            else if (this.isReversing)
            {
                this.isReversing = false;
                var v = GetInputFieldValue(this.currentControl.Key, this.currentControl.Value);
                string inputName;
                bool inputIsAxis, inputIsReversed;
                if (KRSInputAttribute.GetInputDef(v, out inputName, out inputIsAxis, out inputIsReversed))
                {
                    SetInputFieldValue(this.currentControl.Key, this.currentControl.Value,
                        KRSInputAttribute.GetInputString(inputName, inputIsAxis, this.currentReversed));
                }
            }
        }
    }
}
