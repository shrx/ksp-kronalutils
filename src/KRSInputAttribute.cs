using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    /** 
     * <summary>
     * Marks a string field to be the internal name of an input control (e.g. left arrow, gamepad right trigger, etc).
     * </summary>
     */
    public class KRSInputAttribute : KSPField
    {
        private bool canReverse;

        public KRSInputAttribute(string guiName, bool canReverse)
        {
            this.guiActive = false;
            this.isPersistant = true;
            this.guiName = guiName;
            this.canReverse = canReverse;
        }

        /**
         * <summary>
         * Parses a string returned from <see cref="KRSInputAttribute.GetInputString"/>.
         * </summary>
         */
        public static bool GetInputDef(string input, out string name, out bool isAxis, out bool isReversed)
        {
            name = "";
            isAxis = false;
            isReversed = false;
            if (input == "") return false;

            var i = input.Split(',');
            isAxis = i[0] == "Axis";
            isReversed = int.Parse(i[1]) != 0;
            name = i[2];

            return true;
        }

        /**
         * <summary>
         * Returns a string to be used as internal name of an input control.
         * </summary>
         */
        public static string GetInputString(string name, bool isAxis, bool isReversed)
        {
            return (isAxis ? "Axis" : "Key") + "," + (isReversed ? "1" : "0") + "," + name;
        }

        /**
         * <summary>
         * Returns value associated with an input.
         * </summary>
         */
        public static float GetInputValue(string input)
        {
            string name;
            bool isAxis, isReversed;
            if (!GetInputDef(input, out name, out isAxis, out isReversed)) return 0f;

            float v;
            if (isAxis)
            {
                v = Input.GetAxis(name);
            }
            else
            {
                v = Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), name)) ? 1f : 0f;
            }
            if (isReversed) v *= -1;
            return v;
        }
    }
}
