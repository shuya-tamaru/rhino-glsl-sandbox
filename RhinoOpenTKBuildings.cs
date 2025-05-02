using System;
using System.Runtime.InteropServices;
using OpenTK;

namespace BoxClipping.OpenGL
{
    public class RhinoOpenTKBindings : IBindingsContext
    {
        [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress", CharSet = CharSet.Ansi)]
        private static extern IntPtr wglGetProcAddress(string name);

        public IntPtr GetProcAddress(string function)
        {
            IntPtr ptr = wglGetProcAddress(function);
            if (ptr != IntPtr.Zero)
                return ptr;

            IntPtr module = NativeLibrary.Load("opengl32.dll");
            NativeLibrary.TryGetExport(module, function, out ptr);
            return ptr;
        }
    }
}
