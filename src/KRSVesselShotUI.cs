using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KRSVesselShotUI : MonoBehaviour
    {
        private KRSVesselShot control;
        private Rect windowSize;
        private Vector2 windowScrollPos;
        private int tabCurrent;
        private string[] tabNames;
        private Action[] tabGUI;
        private Rect orthoViewRect;
        private GUIStyle guiStyleButtonAlert;

        private bool IsOnEditor()
        {
            return (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH);
        }

        public void Start()
        {
            this.control = new KRSVesselShot();
            this.windowSize = new Rect(256f, 50f, 300f, Screen.height - 50f);
            this.tabNames = new string[] { "View", "Config" };
            this.tabGUI = new Action[] { GUITabView, GUITabConfig };
            this.control.Config.onApply += ConfigApplied;
            this.control.Config.onRevert += ConfigReverted;
            
            EditorLogic.fetch.editorCamera.gameObject.AddComponent<KRSEditorAxis>();
        }

        private void ConfigApplied()
        {
            EditorLogic.fetch.partPanelBtn.controlIsEnabled = true;
            EditorLogic.fetch.actionPanelBtn.controlIsEnabled = true;
            EditorLogic.fetch.crewPanelBtn.controlIsEnabled = true;
            EditorLogic.fetch.saveBtn.controlIsEnabled = false;
            EditorLogic.fetch.launchBtn.controlIsEnabled = false;
            EditorLogic.fetch.exitBtn.controlIsEnabled = false;
            EditorLogic.fetch.loadBtn.controlIsEnabled = true;
            EditorLogic.fetch.newBtn.controlIsEnabled = true;
        }

        private void ConfigReverted()
        {
            EditorLogic.fetch.Unlock();
        }

        public void Update()
        {
            if (this.tabCurrent == 0 && (this.orthoViewRect.width * this.orthoViewRect.height) > 1f)
            {
                this.control.Update(false, (int)this.orthoViewRect.width * 2, (int)this.orthoViewRect.height * 2);
            }
        }

        public void OnGUI()
        {
            if (!IsOnEditor()) return;

            if (this.guiStyleButtonAlert == null)
            {
                this.guiStyleButtonAlert = new GUIStyle(GUI.skin.button);
                this.guiStyleButtonAlert.active.textColor = XKCDColors.BrightRed;
                this.guiStyleButtonAlert.hover.textColor = XKCDColors.Red;
                this.guiStyleButtonAlert.normal.textColor = XKCDColors.DarkishRed;
                this.guiStyleButtonAlert.fontStyle = FontStyle.Bold;
                this.guiStyleButtonAlert.fontSize = 8;
                this.guiStyleButtonAlert.stretchWidth = false;
                this.guiStyleButtonAlert.alignment = TextAnchor.MiddleCenter;
            }

            this.windowSize = GUILayout.Window(GetInstanceID(), this.windowSize, GUIWindow, "Kronal Vessel Viewer", HighLogic.Skin.window);
            EditorLogic.softLock = this.windowSize.Contains(Event.current.mousePosition);
        }

        private void GUIWindow(int id)
        {
            GUILayout.BeginVertical("box");
            GUIButtons();
            this.tabGUI[this.tabCurrent]();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void GUIButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Explode"))
            {
                this.control.UpdateShipBounds();
                this.control.Config.Execute(this.control.Ship);
            }

            if (GUILayout.Button("Revert"))
            {
                this.control.Config.Revert();
            }

            if (GUILayout.Button("Screenshot"))
            {
                this.control.Update();
                this.control.Execute();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ͼ"))
            {
                this.control.direction = Quaternion.AngleAxis(-45f, this.control.Camera.transform.up) * this.control.direction;
            }
            if (GUILayout.RepeatButton("▲"))
            {
                this.control.position.y -= 0.1f;
            }
            if (GUILayout.Button("Ͽ")) //↶
            {
                this.control.direction = Quaternion.AngleAxis(45f, this.control.Camera.transform.up) * this.control.direction;
            }
            if (GUILayout.RepeatButton("ʘ"))
            {
                this.control.position.z += 0.1f;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("◄"))
            {
                this.control.position.x += 0.1f;
            }
            if (GUILayout.RepeatButton("▼"))
            {
                this.control.position.y += 0.1f;
            }
            if (GUILayout.RepeatButton("►"))
            {
                this.control.position.x -= 0.1f;
            }
            if (GUILayout.RepeatButton("Ø"))
            {
                this.control.position.z -= 0.1f;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (GUILayout.Button("RESET", this.guiStyleButtonAlert, GUILayout.ExpandHeight(true)))
            {
                this.control.direction = Vector3.forward;
                this.control.position = Vector3.zero;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.control.Orthographic = GUILayout.Toggle(this.control.Orthographic, "Ortho");
            this.control.EffectsAntiAliasing = GUILayout.Toggle(this.control.EffectsAntiAliasing, "AA");
            GUILayout.EndHorizontal();

            for (var i = 0; i < this.control.MaterialBluePrint.PropertyCount; ++i)
            {
                var prop = this.control.MaterialBluePrint[i];
                prop.Match(
                    IfFloat: (p) => {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        p.Value = GUILayout.HorizontalSlider(p.Value, p.RangeMin, p.RangeMax);
                        GUILayout.Label(p.Value.ToString("F"), GUILayout.Width(30f));
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfColor: (p) => {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Color oldVal = p.Value, newVal;
                        newVal.r = GUILayout.HorizontalSlider(oldVal.r, 0f, 1f);
                        newVal.g = GUILayout.HorizontalSlider(oldVal.g, 0f, 1f);
                        newVal.b = GUILayout.HorizontalSlider(oldVal.b, 0f, 1f);
                        newVal.a = 1f;
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.r.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.g.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.b.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfVector: (p) => {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Vector4 oldVal = p.Value, newVal;
                        newVal.x = GUILayout.HorizontalSlider(oldVal.x, 0f, 1f);
                        newVal.y = GUILayout.HorizontalSlider(oldVal.y, 0f, 1f);
                        newVal.z = GUILayout.HorizontalSlider(oldVal.z, 0f, 1f);
                        newVal.w = GUILayout.HorizontalSlider(oldVal.w, 0f, 1f);
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.x.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.y.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.z.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.w.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", this.guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    });               
            }

            GUILayout.BeginHorizontal();
            this.tabCurrent = GUILayout.Toolbar(this.tabCurrent, this.tabNames);
            GUILayout.EndHorizontal();
        }

        private void GUITabView()
        {
            var r = GUILayoutUtility.GetRect(0, this.windowSize.width, 0, this.windowSize.height);
            if (Event.current.type == EventType.Repaint)
            {
                this.orthoViewRect = r;
            }
            var texture = this.control.Texture();
            if (texture)
            {
                GUI.DrawTexture(this.orthoViewRect, texture, ScaleMode.ScaleToFit, false);
            }
        }

        private void GUITabConfig()
        {
            this.windowScrollPos = GUILayout.BeginScrollView(this.windowScrollPos, false, true);
            foreach (var ol in this.control.Config.Config)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + ol.Name + "</b>", "box");
                foreach (var o in ol.Options)
                {
                    GUILayout.BeginHorizontal();
                    if (o.IsToggle)
                    {
                        o.valueActive = GUILayout.Toggle(o.valueActive, o.Name);
                    }
                    else
                    {
                        GUILayout.Label(o.Name);
                    }
                    if (o.HasParam)
                    {
                        var displayText = o.valueParam.ToString(o.valueFormat);
                        displayText = GUILayout.TextField(displayText);
                        float value;
                        if (float.TryParse(displayText, out value))
                        {
                            o.valueParam = value;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
        }
    }
}
