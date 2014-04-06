using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    class VesselElementViewOption
    {
        public string Name { get; private set; }
        public bool IsToggle { get; private set; }
        public bool HasParam { get; private set; }
        public Action<VesselElementViewOptions, VesselElementViewOption, Part> Apply { get; private set; }
        public bool valueActive;
        public float valueParam;
        public string valueFormat;

        public VesselElementViewOption(string name, bool isToggle, bool hasParam,
            Action<VesselElementViewOptions, VesselElementViewOption, Part> apply,
            bool defaultValueActive = false, float defaultValueParam = 0f,
            string valueFormat = "F2")
        {
            this.Name = name;
            this.IsToggle = isToggle;
            this.HasParam = hasParam;
            this.Apply = apply;
            this.valueActive = defaultValueActive;
            this.valueParam = defaultValueParam;
            this.valueFormat = valueFormat;
        }
    }

    class VesselElementViewOptions
    {
        public string Name { get; private set; }
        public Func<Part, bool> CanApply { get; private set; }
        public List<VesselElementViewOption> Options { get; private set; }

        public VesselElementViewOptions(string name, Func<Part, bool> canApply)
        {
            this.Name = name;
            this.CanApply = canApply;
            this.Options = new List<VesselElementViewOption>();
        }


        internal void Apply(Part part)
        {
            if (!this.CanApply(part)) return;

            foreach (var option in this.Options)
            {
                if (option.valueActive)
                {
                    option.Apply(this, option, part);
                }
            }
        }
    }

    class VesselViewConfig
    {
        private Dictionary<Transform, Vector3> positions;
        private Dictionary<Renderer, bool> visibility;
        private Dictionary<Part, bool> freezed;
        private IShipconstruct ship;
        public List<VesselElementViewOptions> Config { get; private set; }
        public Action onApply;
        public Action onRevert;

        public VesselViewConfig()
        {
            this.positions = new Dictionary<Transform, Vector3>();
            this.visibility = new Dictionary<Renderer, bool>();
            this.freezed = new Dictionary<Part, bool>();
            this.onApply = () => { };
            this.onRevert = () => { };
            this.Config = new List<VesselElementViewOptions>() {
                new VesselElementViewOptions("Stack Decouplers/Separators", CanApplyIfModule("ModuleDecouple")) {
                    Options = {
                        new VesselElementViewOption("Explode", true, true, StackDecouplerExplode, false, 1f),
                    }
                },
                new VesselElementViewOptions("Radial Decouplers/Separators", CanApplyIfModule("ModuleAnchoredDecoupler")) {
                    Options = {
                        new VesselElementViewOption("Explode", true, true, RadialDecouplerExplode, false, 1f),
                    }
                },
                new VesselElementViewOptions("Docking Ports", CanApplyIfModule("ModuleDockingNode")) {
                    Options = {
                        new VesselElementViewOption("Explode", true, true, DockingPortExplode, false, 1f),
                    }
                },
                new VesselElementViewOptions("Engine Fairings", CanApplyIfModule("ModuleJettison")) {
                    Options = {
                        new VesselElementViewOption("Explode", true, true, EngineFairingExplode, false, 1f),
                        new VesselElementViewOption("Hide", true, false, EngineFairingHide, false),
                    }
                },
                new VesselElementViewOptions("Procedural Fairings", CanApplyIfModule("ProceduralFairingSide")) {
                    Options = {
                        new VesselElementViewOption("Explode", true, true, ProcFairingExplode, false, 1f),
                        new VesselElementViewOption("Hide", true, false, PartHide, false),
                        new VesselElementViewOption("Hide front half", true, false, ProcFairingHide, false),
                    }
                },
                new VesselElementViewOptions("Struts", CanApplyIfType("StrutConnector")) {
                    Options = {
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, false),
                    }
                }
            };
        }

        private void SaveState()
        {
            this.positions.Clear();
            this.visibility.Clear();
            this.freezed.Clear();
            var p = EditorLogic.startPod;
            foreach (var t in p.GetComponentsInChildren<Transform>())
            {
                this.positions[t] = t.localPosition;
            }
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                this.visibility[r] = r.enabled;
            }
            foreach (var part in this.ship.Parts)
            {
                this.freezed[part] = part.frozen;
            }
        }

        public void Revert()
        {
            var p = EditorLogic.startPod;
            foreach (var t in p.GetComponentsInChildren<Transform>())
            {
                if (this.positions.ContainsKey(t))
                {
                    t.localPosition = this.positions[t];
                }
            }
                
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                if (this.visibility.ContainsKey(r))
                {
                    r.enabled = this.visibility[r];
                }
            }

            foreach (var part in this.ship.Parts)
            {
                if (this.freezed.ContainsKey(part))
                {
                    part.frozen = this.freezed[part];
                }
            }

            this.onRevert();
        }

        public void Execute(IShipconstruct ship)
        {
            this.ship = ship;
            Revert();
            SaveState();
            foreach (var part in ship.Parts)
            {
                foreach (var c in this.Config)
                {
                    c.Apply(part);
                }
                part.frozen = true;
            }
            
            this.onApply();
        }

        private Func<Part, bool> CanApplyIfType(string typeName)
        {
            var type = KRSUtils.FindType(typeName);
            return (p) => type.IsInstanceOfType(p);
        }

        private Func<Part, bool> CanApplyIfModule(string moduleName)
        {
            return (p) => p.Modules.Contains(moduleName);
        }

        private void PartHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Hiding Part " + part.ToString());
            foreach (var r in part.GetComponents<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void PartHideRecursive(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Hiding Part " + part.ToString());
            foreach (var r in part.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

        private void StackDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Exploding Stack Decoupler: " + part.ToString());
            var module = part.Module<ModuleDecouple>();
            if (module.isDecoupled) return;
            if (!part.parent) return;
            Vector3 dir;
            if (module.isOmniDecoupler)
            {
                foreach (var c in part.children)
                {
                    dir = Vector3.Normalize(c.transform.position - part.transform.position);
                    c.transform.Translate(dir * o.valueParam, Space.World);
                }
            }
            dir = Vector3.Normalize(part.transform.position - part.parent.transform.position);
            part.transform.Translate(dir * o.valueParam, Space.World);
        }


        private void RadialDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Exploding Radial Decoupler: " + part.ToString());
            var module = part.Module<ModuleAnchoredDecoupler>();
            if (module.isDecoupled) return;
            if (string.IsNullOrEmpty(module.explosiveNodeID)) return;
            var an = module.explosiveNodeID == "srf" ? part.srfAttachNode : part.findAttachNode(module.explosiveNodeID);
            if (an == null || an.attachedPart == null) return;
            var distance = o.valueParam;
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(part.transform.right * distance, Space.World);
        }

        private void DockingPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Exploding Docking Port: " + part.ToString());
            var module = part.Module<ModuleDockingNode>();
            if (string.IsNullOrEmpty(module.referenceAttachNode)) return;
            var an = part.findAttachNode(module.referenceAttachNode);
            if (!an.attachedPart) return;
            var distance = o.valueParam;
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(module.nodeTransform.forward * distance, Space.World);
        }

        private void EngineFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Exploding Engine Fairing: " + part.ToString());
            var module = part.Module<ModuleJettison>();
            if (!module.isJettisoned)
            {
                if (!module.isFairing)
                {
                    module.jettisonTransform.Translate(module.jettisonDirection * o.valueParam);
                }
            }
        }

        private void EngineFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Hiding Engine Fairing: " + part.ToString());
            var module = part.Module<ModuleJettison>();
            if (module.jettisonTransform)
            {
                foreach (var r in module.jettisonTransform.gameObject.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }
        }

        private void ProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Exploding Procedural Fairing: " + part.ToString());
            var nct = part.FindModelTransform("nose_collider");
            if (!nct) return;
            MeshFilter mf;
            Vector3 extents = (mf = part.gameObject.GetComponentInChildren<MeshFilter>()) ? mf.mesh.bounds.size : new Vector3(o.valueParam, o.valueParam, o.valueParam);
            part.transform.Translate(Vector3.Scale(nct.right, extents), Space.World);
        }

        private void ProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            MonoBehaviour.print("Hiding Procedural Fairing: " + part.ToString());
            var nct = part.FindModelTransform("nose_collider");
            if (!nct) return;
            var forward = EditorLogic.startPod.transform.forward;
            var right = EditorLogic.startPod.transform.right;
            if (Vector3.Dot(nct.right, -(forward + right).normalized) > 0f)
            {
                var renderer = part.GetComponentInChildren<Renderer>();
                if (renderer) renderer.enabled = false;
            }
        }
    }

}
