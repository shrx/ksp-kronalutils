using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    static class KRSHelper
    {
        public static T Module<T>(this Part part) where T : PartModule
        {
            return (T)part.Modules[typeof(T).Name];
        }

        private static Stack<KeyValuePair<string, Stopwatch>> dbgStack = new Stack<KeyValuePair<string, Stopwatch>>();

        public static void DbgBegin(this object self, string name = "")
        {
            Stopwatch stopwatch = new Stopwatch();
            dbgStack.Push(new KeyValuePair<string, Stopwatch>(name, stopwatch));
            stopwatch.Start();
        }

        public static void DbgEnd(this object self)
        {
            var e = dbgStack.Pop();
            var name = e.Key;
            var stopwatch = e.Value;
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
            MonoBehaviour.print("[DEBUG] Event: " + name + "   DT: " + elapsedTime);
        }
    }

    class KRSUtils
    {
        public static Type FindType(string qualifiedTypeName)
        {
            Type t = Type.GetType(qualifiedTypeName);

            if (t != null)
            {
                return t;
            }
            else
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(qualifiedTypeName);
                    if (t != null)
                        return t;
                }
                return null;
            }
        }

        public static string GetResourceString(string name)
        {
            if (KSP.IO.File.Exists<KRSUtils>(name))
            {
                return KSP.IO.File.ReadAllText<KRSUtils>(name);
            }
            else
            {
                return Properties.Resources.ResourceManager.GetString(name);
            }
        }

        public static Vector3 ProjectVectorToPlane(Vector3 v, Vector3 planeNormal)
        {
            return v - Vector3.Dot(v, planeNormal) * planeNormal;
        }

        public static Vector3 VectorSwap(Vector3 v)
        {
            //return new Vector3(v.y, v.x, v.z);
            return new Vector3(v.y, v.z, v.x);
        }

        public static float VectorSignedAngle(Vector3 a, Vector3 b, Vector3 planeNormal)
        {
            var angle = Vector3.Angle(a, b);
            return Vector3.Dot(Vector3.Cross(planeNormal, a), b) >= 0f ? 360f - angle : angle;
        }

        public static float Wrap(float value, float min, float max)
        {
            return (((value - min) % (max - min)) + (max - min)) % (max - min) + min;
        }

        public static KeyCode[] BindableKeys = {
            KeyCode.Alpha0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
            KeyCode.Keypad0,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6,
            KeyCode.Keypad7,
            KeyCode.Keypad8,
            KeyCode.Keypad9,
            KeyCode.Joystick1Button0,
            KeyCode.Joystick1Button1,
            KeyCode.Joystick1Button2,
            KeyCode.Joystick1Button3,
            KeyCode.Joystick1Button4,
            KeyCode.Joystick1Button5,
            KeyCode.Joystick1Button6,
            KeyCode.Joystick1Button7,
            KeyCode.Joystick1Button8,
            KeyCode.Joystick1Button9,
            KeyCode.Joystick1Button10,
            KeyCode.Joystick1Button11,
            KeyCode.Joystick1Button12,
            KeyCode.Joystick1Button13,
            KeyCode.Joystick1Button14,
            KeyCode.Joystick1Button15,
            KeyCode.Joystick1Button16,
            KeyCode.Joystick1Button17,
            KeyCode.Joystick1Button18,
            KeyCode.Joystick1Button19,
            KeyCode.Joystick2Button0,
            KeyCode.Joystick2Button1,
            KeyCode.Joystick2Button2,
            KeyCode.Joystick2Button3,
            KeyCode.Joystick2Button4,
            KeyCode.Joystick2Button5,
            KeyCode.Joystick2Button6,
            KeyCode.Joystick2Button7,
            KeyCode.Joystick2Button8,
            KeyCode.Joystick2Button9,
            KeyCode.Joystick2Button10,
            KeyCode.Joystick2Button11,
            KeyCode.Joystick2Button12,
            KeyCode.Joystick2Button13,
            KeyCode.Joystick2Button14,
            KeyCode.Joystick2Button15,
            KeyCode.Joystick2Button16,
            KeyCode.Joystick2Button17,
            KeyCode.Joystick2Button18,
            KeyCode.Joystick2Button19,
            KeyCode.Joystick3Button0,
            KeyCode.Joystick3Button1,
            KeyCode.Joystick3Button2,
            KeyCode.Joystick3Button3,
            KeyCode.Joystick3Button4,
            KeyCode.Joystick3Button5,
            KeyCode.Joystick3Button6,
            KeyCode.Joystick3Button7,
            KeyCode.Joystick3Button8,
            KeyCode.Joystick3Button9,
            KeyCode.Joystick3Button10,
            KeyCode.Joystick3Button11,
            KeyCode.Joystick3Button12,
            KeyCode.Joystick3Button13,
            KeyCode.Joystick3Button14,
            KeyCode.Joystick3Button15,
            KeyCode.Joystick3Button16,
            KeyCode.Joystick3Button17,
            KeyCode.Joystick3Button18,
            KeyCode.Joystick3Button19,
            KeyCode.Joystick4Button0,
            KeyCode.Joystick4Button1,
            KeyCode.Joystick4Button2,
            KeyCode.Joystick4Button3,
            KeyCode.Joystick4Button4,
            KeyCode.Joystick4Button5,
            KeyCode.Joystick4Button6,
            KeyCode.Joystick4Button7,
            KeyCode.Joystick4Button8,
            KeyCode.Joystick4Button9,
            KeyCode.Joystick4Button10,
            KeyCode.Joystick4Button11,
            KeyCode.Joystick4Button12,
            KeyCode.Joystick4Button13,
            KeyCode.Joystick4Button14,
            KeyCode.Joystick4Button15,
            KeyCode.Joystick4Button16,
            KeyCode.Joystick4Button17,
            KeyCode.Joystick4Button18,
            KeyCode.Joystick4Button19,           
            KeyCode.Mouse2,
            KeyCode.Mouse3,
            KeyCode.Mouse4,
            KeyCode.Mouse5,
            KeyCode.Mouse6,
        };
        public static string AxisFormat = "joy{0}.{1}";
    }
}
