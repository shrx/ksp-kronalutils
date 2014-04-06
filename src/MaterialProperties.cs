using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KronalUtils
{
    abstract class ShaderMaterialProperty
    {
        private ShaderMaterialProperty(Material material, string name, string displayName)
        {
            this.Material = material;
            this.Name = name;
            this.DisplayName = displayName;
        }

        public readonly Material Material;
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public abstract void Match(
            Action<FloatProperty> IfFloat = null,
            Action<VectorProperty> IfVector = null,
            Action<ColorProperty> IfColor = null,
            Action<TextureProperty> IfTexture = null);

        public abstract ShaderMaterialProperty Clone();
        
        public sealed class FloatProperty : ShaderMaterialProperty
        {
            public float Value
            {
                get { return this.Material.GetFloat(this.Name); }
                set { this.Material.SetFloat(this.Name, value); }
            }
            public readonly float RangeMin;
            public readonly float RangeMax;
            public readonly float DefaultValue;

            internal FloatProperty(Material material, string name, string displayName, float min, float max)
                : base(material, name, displayName)
            {
                this.RangeMin = min;
                this.RangeMax = max;
                this.DefaultValue = this.Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfFloat != null) IfFloat(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new FloatProperty(this.Material, this.Name, this.DisplayName, this.RangeMin, this.RangeMax);      
            }
        }

        public sealed class VectorProperty : ShaderMaterialProperty
        {
            public Vector4 Value
            {
                get { return this.Material.GetVector(this.Name); }
                set { this.Material.SetVector(this.Name, value); }
            }
            public readonly Vector4 DefaultValue;

            internal VectorProperty(Material material, string name, string displayName)
                : base(material, name, displayName)
            {
                this.DefaultValue = this.Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfVector != null) IfVector(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new VectorProperty(this.Material, this.Name, this.DisplayName);
            }
        }

        public sealed class ColorProperty : ShaderMaterialProperty
        {
            public Color Value
            {
                get { return this.Material.GetColor(this.Name); }
                set { this.Material.SetColor(this.Name, value); }
            }
            public readonly Color DefaultValue;

            internal ColorProperty(Material material, string name, string displayName)
                : base(material, name, displayName)
            {
                this.DefaultValue = this.Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfColor != null) IfColor(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new VectorProperty(this.Material, this.Name, this.DisplayName);
            }
        }

        public sealed class TextureProperty : ShaderMaterialProperty
        {
            public Texture Value
            {
                get { return this.Material.GetTexture(this.Name); }
                set { this.Material.SetTexture(this.Name, value); }
            }

            internal TextureProperty(Material material, string name, string displayName)
                : base(material, name, displayName) { }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfTexture != null) IfTexture(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new TextureProperty(this.Material, this.Name, this.DisplayName);
            }
        }
    }

    class ShaderMaterial
    {
        public Material Material { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public bool Enabled { get; set; }
        private List<ShaderMaterialProperty> properties;
        private Dictionary<string, ShaderMaterialProperty> propertiesByName;
        public ShaderMaterialProperty this[int propertyIndex]
        {
            get { return this.properties[propertyIndex]; }
            set { this.properties[propertyIndex] = value; }
        }
        public ShaderMaterialProperty this[string propertyName]
        {
            get { return this.propertiesByName[propertyName]; }
            set { this.propertiesByName[propertyName] = value; }
        }
        public int PropertyCount { get { return this.properties.Count; } }

        private ShaderMaterial()
        {
            this.properties = new List<ShaderMaterialProperty>();
            this.propertiesByName = new Dictionary<string, ShaderMaterialProperty>();
            this.Enabled = true;
        }


        public ShaderMaterial(string contents)
            : this()
        {
            this.Material = new Material(contents);
            var p = @"Properties\s*\{[^\{\}]*(((?<Open>\{)[^\{\}]*)+((?<Close-Open>\})[^\{\}]*)+)*(?(Open)(?!))\}";
            var m = Regex.Match(contents, p, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                throw new Exception("Error parsing shader properties: " + this.Material.shader.name);
            }
            p = @"(?<name>\w*)\s*\(\s*""(?<displayname>[^""]*)""\s*,\s*(?<type>Float|Vector|Color|2D|Rect|Cube|Range\s*\(\s*(?<rangemin>[\d.]*)\s*,\s*(?<rangemax>[\d.]*)\s*\))\s*\)";
            MonoBehaviour.print("1 " + m.Value);
            foreach(Match match in Regex.Matches(m.Value, p))
            {
                ShaderMaterialProperty prop;
                var name = match.Groups["name"].Value;
                var displayname = match.Groups["displayname"].Value;
                var typestr = match.Groups["type"].Value;
                switch (typestr.ToUpperInvariant())
                {
                    case "VECTOR":
                        prop = new ShaderMaterialProperty.VectorProperty(this.Material, name, displayname);
                        break;
                    case "COLOR":
                        prop = new ShaderMaterialProperty.ColorProperty(this.Material, name, displayname);
                        break;
                    case "2D":
                    case "RECT":
                    case "CUBE":
                        prop = new ShaderMaterialProperty.TextureProperty(this.Material, name, displayname);
                        break;
                    default: /// Defaults to Range(*,*)
                        prop = new ShaderMaterialProperty.FloatProperty(this.Material, name, displayname, float.Parse(match.Groups["rangemin"].Value), float.Parse(match.Groups["rangemax"].Value));
                        break;
                }
                this.properties.Add(prop);
                this.propertiesByName[prop.Name] = prop;
            }
        }

        public ShaderMaterial Clone()
        {
            var result = new ShaderMaterial();
            result.Material = new Material(this.Material);
            foreach (var p in this.properties)
            {
                result.properties.Add(p.Clone());
            }
            return result;
        }
    }
}
