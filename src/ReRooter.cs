using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    //[KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class ReRooter: MonoBehaviour
    {
        private Part activePart;

        private bool IsOnEditor()
        {
            return (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH);
        }

        private Rect btnMakeRoot = new Rect(Screen.width - 100, 50, 70, 36);

        private void OnGUI()
        {
            if (!IsOnEditor()) return;

            if (EditorLogic.SelectedPart != null) activePart = EditorLogic.SelectedPart;

            if (activePart != null)
            {
                GUI.Label(btnMakeRoot, activePart.name);
                if (Input.GetKeyDown(KeyCode.T))
                {
                    print("clicked!");
                    try
                    {
                        MakeRoot(activePart);
                    }
                    catch
                    {
                        print("ERROR");
                    }
                }
            }
            else
            {
                GUI.Label(btnMakeRoot, "NONE");
            }
        }

        private void MakeRoot(Part part)
        {
            print(part.vessel.ToString());
            print(part.vessel.rootPart.ToString());
            print(part.vessel.rootPart.transform.ToString());
            print(part.vessel.rootPart.transform.parent.ToString());
            SetParent(part.vessel.rootPart.transform.parent, part, null, 0);
            //EditorLogic.startPod.SetHierarchyRoot(part);
            EditorLogic.startPod = part;
            part.vessel.rootPart = part;
        }

        //void SetParent(Part part, Part newParent, int n) {
        //    print(n.ToString() + "  " + part.name + " <- " + (newParent != null?newParent.name:"null"));
        //    if (part.parent != null) {
        //        SetParent(part.parent, part, n + 1);
        //        part.onDetach();
        //        part.parent.children.Remove(part);
        //        part.parent.onAttach(part);
        //        part.children.Add(part.parent);
        //    }
        //    part.onAttach(newParent, newParent = null);
        //    part.parent = newParent;
        //    part.isAttached = part.parent != null;
        //}

        void SetParent(Transform rootParent, Part part, Part newParent, int n)
        {
            print(n.ToString() + "  " + part.name + " <- " + (newParent != null ? newParent.name : "null"));
            if (part.parent != null)
            {
                SetParent(rootParent, part.parent, part, n + 1);
                part.parent.children.Remove(part);
                part.onDetach();
                part.parent.onAttach(part);
                part.children.Add(part.parent);
                part.parent.transform.parent = part.attachJoint.transform;
            }
            if (newParent == null || newParent.attachJoint == null)
            {
                part.transform.parent = rootParent;
            }
            else
            {
                part.transform.parent = newParent.attachJoint.transform;
            }
            part.onAttach(newParent, newParent = null);
            part.parent = newParent;
            part.isAttached = part.parent != null;
        }
    }
}
