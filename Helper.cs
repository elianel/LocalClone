using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;

namespace LocalClone
{
    internal class Helper
    {
        public static unsafe T Patch<T>(MethodInfo targetMethod, MethodInfo patch) where T : Delegate
        {
            var method = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(targetMethod).GetValue(null);
            MelonUtils.NativeHookAttach((IntPtr)(&method), patch!.MethodHandle.GetFunctionPointer());
            return Marshal.GetDelegateForFunctionPointer<T>(method);
        }
    }
}
