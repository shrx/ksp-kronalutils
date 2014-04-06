using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    class KRSVesselShot
    {
        public ShaderMaterial MaterialFXAA = new ShaderMaterial(KronalUtils.Properties.Resources.ShaderFXAA);
        public ShaderMaterial MaterialColorAdjust = new ShaderMaterial(KSP.IO.File.ReadAllText<KRSVesselShot>("coloradjust"));
        public ShaderMaterial MaterialEdgeDetect = new ShaderMaterial(KSP.IO.File.ReadAllText<KRSVesselShot>("edn2"));
        public ShaderMaterial MaterialBluePrint = new ShaderMaterial(KSP.IO.File.ReadAllText<KRSVesselShot>("blueprint"));
        private List<string> Shaders = new List<string>() { "edn", "cutoff", "diffuse", "bumped", "bumpedspecular", "specular", "unlit", "emissivespecular", "emissivebumpedspecular" };
        private Dictionary<string, Material> Materials;
        public readonly List<ShaderMaterial> Effects;

        private Camera[] cameras;
        private RenderTexture rt;
        private int maxWidth = 9999;
        private int maxHeight = 5000;
        private Bounds shipBounds;
        internal Camera Camera { get; private set; }
        internal Vector3 direction;
        internal Vector3 position;
        internal bool EffectsAntiAliasing { get; set; }
        internal bool Orthographic
        {
            get
            {
                return this.Camera == this.cameras[0];
            }
            set
            {
                this.Camera = this.cameras[value ? 0 : 1];
            }
        }
        internal VesselViewConfig Config { get; private set; }
        internal IShipconstruct Ship
        {
            get
            {
                if (EditorLogic.fetch)
                {
                    return EditorLogic.fetch.ship;
                }
                else
                {
                    return null;
                }
            }
        }

        public KRSVesselShot()
        {
            SetupCameras();
            this.Config = new VesselViewConfig();
            this.direction = Vector3.forward;
            this.Materials = new Dictionary<string, Material>();
            this.Effects = new List<ShaderMaterial>() {
                MaterialColorAdjust,
                MaterialEdgeDetect,
                //MaterialBluePrint,
                //MaterialFXAA
            };
            LoadShaders();
            UpdateShipBounds();

            GameEvents.onPartAttach.Add(PartAttached);
            GameEvents.onPartRemove.Add(PartRemoved);
        }

        ~KRSVesselShot()
        {
            GameEvents.onPartAttach.Remove(PartAttached);
            GameEvents.onPartRemove.Remove(PartRemoved);
        }

        private void SetupCameras()
        {
            this.cameras = new Camera[2];
            this.cameras[0] = new GameObject().AddComponent<Camera>();
            this.cameras[0].enabled = false;
            this.cameras[0].orthographic = true;
            this.cameras[0].cullingMask = EditorLogic.fetch.editorCamera.cullingMask & ~(1 << 16); /// hides kerbals
            this.cameras[0].transparencySortMode = TransparencySortMode.Orthographic;
            this.cameras[1] = new GameObject().AddComponent<Camera>();
            this.cameras[1].enabled = false;
            this.cameras[1].orthographic = false;
            this.cameras[1].cullingMask = this.cameras[0].cullingMask;
            this.Camera = this.cameras[0];
        }

        private void LoadShaders()
        {
            foreach (var shaderFilename in Shaders)
            {
                try
                {
                    var mat = new Material(KRSUtils.GetResourceString(shaderFilename));
                    Materials[mat.shader.name] = mat;
                }
                catch
                {
                    MonoBehaviour.print("[ERROR] " + this.GetType().Name + " : Failed to load " + shaderFilename);
                }
            }
        }

        private void ReplacePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat;
                if (Materials.TryGetValue(r.material.shader.name, out mat))
                {
                    r.material.shader = mat.shader;
                }
                else
                {
                    MonoBehaviour.print("[Warning] " + this.GetType().Name + "No replacement for " + r.material.shader + " in " + part + "/*/" + r);
                }
            }
        }

        private void PartAttached(GameEvents.HostTargetAction<Part, Part> data)
        {
            ReplacePartShaders(data.host);
            ReplacePartShaders(data.target);
            UpdateShipBounds();
        }

        private void PartRemoved(GameEvents.HostTargetAction<Part, Part> data)
        {
            UpdateShipBounds();
        }

        internal void UpdateShipBounds()
        {
            if ((this.Ship != null) && (this.Ship.Parts.Count > 0))
            {
                this.shipBounds = CalcShipBounds();
            }
            else
            {
                this.shipBounds = new Bounds(EditorLogic.fetch.editorBounds.center, Vector3.zero);
            }
            this.shipBounds.Expand(1f);
        }

        private Bounds CalcShipBounds()
        {
            Bounds result = new Bounds(this.Ship.Parts[0].transform.position, Vector3.zero);
            foreach (var current in this.Ship.Parts)
            {
                if (current.collider && !current.Modules.Contains("LaunchClamp"))
                {
                    result.Encapsulate(current.collider.bounds);
                }
            }
            return result;
        }

        public Vector3 GetShipSize()
        {
            return CalcShipBounds().size;
        }

        public void GenTexture(Vector3 direction, int imageWidth = -1, int imageHeight = -1)
        {
            var minusDir = -direction;

            this.Camera.clearFlags = CameraClearFlags.SolidColor;
            this.Camera.backgroundColor = new Color(1f, 1f, 1f, 0.0f);
            this.Camera.transform.position = this.shipBounds.center;
            this.Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.up);
            this.Camera.transform.Translate(Vector3.Scale(minusDir, this.shipBounds.extents) + minusDir * this.Camera.nearClipPlane);
            this.Camera.transform.LookAt(this.shipBounds.center);

            var tangent = this.Camera.transform.up;
            var binormal = this.Camera.transform.right;
            var height = Vector3.Scale(tangent, this.shipBounds.size).magnitude;
            var width = Vector3.Scale(binormal, this.shipBounds.size).magnitude;
            var depth = Vector3.Scale(minusDir, this.shipBounds.size).magnitude;

            float positionOffset;
            if (this.Orthographic)
            {
                this.Camera.transform.Translate(Vector3.Scale(this.position, new Vector3(1f, 1f, 0f)));
                this.Camera.orthographicSize = (height - this.position.z) / 2f;
                positionOffset = 0f;
            }
            else
            {
                positionOffset = (height - this.position.z) / (2f * Mathf.Tan(Mathf.Deg2Rad * this.Camera.fieldOfView / 2f)) - depth * 0.5f;
                this.Camera.transform.Translate(new Vector3(this.position.x, this.position.y, -positionOffset));
            }
            this.Camera.farClipPlane = Camera.nearClipPlane + positionOffset + this.position.magnitude + depth;

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                this.Camera.aspect = width / height;
                imageHeight = (int)Mathf.Clamp(100f * height, 0f, Math.Min(maxHeight, maxWidth / this.Camera.aspect));
                imageWidth = (int)(imageHeight * this.Camera.aspect);
            }
            else
            {
                this.Camera.aspect = (float) imageWidth / (float) imageHeight;
            }
            if (this.rt) RenderTexture.ReleaseTemporary(this.rt);
            this.rt = RenderTexture.GetTemporary(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            this.Camera.targetTexture = this.rt;
            this.Camera.depthTextureMode = DepthTextureMode.DepthNormals;
            this.Camera.Render();
            this.Camera.targetTexture = null;
            //Graphics.Blit(this.rt, this.rt, MaterialColorAdjust.Material);
            //Graphics.Blit(this.rt, this.rt, MaterialEdgeDetect.Material);
            foreach (var fx in Effects)
            {
                if (fx.Enabled)
                {
                    Graphics.Blit(this.rt, this.rt, fx.Material);
                }
            }
        }

        private void SaveTexture(String prefix)
        {
            Texture2D screenShot = new Texture2D(this.rt.width, this.rt.height, TextureFormat.RGB24, false);
            var saveRt = RenderTexture.active;
            RenderTexture.active = this.rt;
            screenShot.ReadPixels(new Rect(0, 0, this.rt.width, this.rt.height), 0, 0);
            screenShot.Apply();
            RenderTexture.active = saveRt; 
            byte[] bytes = screenShot.EncodeToPNG();
            string filename = Path.Combine(Application.dataPath, "Screenshots" + Path.PathSeparator + prefix + "_screenshot.png");
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }

        public void Execute() {
            if (!((EditorLogic.startPod) && (this.Ship != null)))
            {
                return;
            }

            SaveTexture("front");
        }

        public void Update(bool explode = false, int width = -1, int height = -1)
        {
            if (!((EditorLogic.startPod) && (this.Ship != null)))
            {
                return;
            }

            var dir = EditorLogic.startPod.transform.TransformDirection(this.direction);
            if (explode)
            {
                this.Config.Execute(this.Ship);
            }
            try
            {
                GenTexture(dir, width, height);
            }
            finally
            {
                if (explode)
                {
                    this.Config.Revert();
                }
            }
        }

        internal Texture Texture()
        {
            if (!((EditorLogic.startPod) && (this.Ship != null)))
            {
                return null;
            }
            else
            {
                return this.rt;
            }
        }
    }
}
