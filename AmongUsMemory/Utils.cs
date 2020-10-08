﻿using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace AmongUsMemory
{
    public static class Utils
    {
        static Dictionary<(Type, string), int> _offsetMap = new Dictionary<(Type, string), int>();

        public static T FromBytes<T>(byte[] bytes)
        {
            GCHandle gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T data = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();
            return data;
        }

        public static int SizeOf<T>()
        {
            return Marshal.SizeOf(typeof(T));
        }

        public static string GetAddress(this long value) { return value.ToString("X"); }
        public static string GetAddress(this int value) { return value.ToString("X"); }
        public static string GetAddress(this uint value) { return value.ToString("X"); }
        public static string GetAddress(this IntPtr value) { return value.ToInt32().GetAddress(); }
        public static string GetAddress(this UIntPtr value) { return value.ToUInt32().GetAddress(); }

        public static IntPtr Sum(this IntPtr ptr, IntPtr ptr2) { return (IntPtr)(ptr.ToInt32() + ptr2.ToInt32()); }
        public static IntPtr Sum(this IntPtr ptr, UIntPtr ptr2) { return (IntPtr)(ptr.ToInt32() + (int)ptr2.ToUInt32()); }
        public static IntPtr Sum(this UIntPtr ptr, IntPtr ptr2) { return (IntPtr)(ptr.ToUInt32() + ptr2.ToInt32()); }
        public static IntPtr Sum(this int ptr, IntPtr ptr2) { return (IntPtr)(ptr + ptr2.ToInt32()); }
        public static IntPtr Sum(this IntPtr ptr, int ptr2) { return (IntPtr)(ptr.ToInt32() + ptr2); }

        public static IntPtr GetMemberPointer(IntPtr basePtr, Type type, string fieldName)
        {
            int offset = GetOffset(type, fieldName); 
            return basePtr.Sum(offset);
        }
        public static int GetOffset(Type type, string fieldName)
        {
            if (_offsetMap.ContainsKey((type, fieldName)))
            {
                return _offsetMap[(type, fieldName)];
            }
            FieldInfo field = type.GetField(fieldName);
            object[] atts = field.GetCustomAttributes(true);
            foreach (object att in atts)
            {
                if (att.GetType() == typeof(FieldOffsetAttribute))
                {
                    _offsetMap.Add((type, fieldName), (att as FieldOffsetAttribute).Value);
                    return (att as FieldOffsetAttribute).Value;
                }
            }

            return -1;
        }

        /// <summary>
        /// Support All Language.
        /// </summary> 
        public static string ReadString(IntPtr offset)
        {
            //string pointer + 8 = length
            int length = AmongUsMemory.Main.mem.ReadInt(offset.Sum(8).GetAddress());

            //unit of string is 2byte.
            int format_length = length * 2;

            //string pointer + 12 = value
            byte[] strByte = AmongUsMemory.Main.mem.ReadBytes(offset.Sum(12).GetAddress(), format_length); 

            StringBuilder sb = new StringBuilder(); 
            for (int i = 0; i < strByte.Length; i += 2)
            {
                // english = 1byte
                if (strByte[i + 1] == 0) 
                    sb.Append((char)strByte[i]); 
                // korean & unicode = 2byte
                else
                    sb.Append(System.Text.Encoding.Unicode.GetString(new byte[] { strByte[i], strByte[i + 1] }));
            }

            return sb.ToString();
        }

    }
}
