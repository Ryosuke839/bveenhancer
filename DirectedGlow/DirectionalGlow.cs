using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlimDX;
using SlimDX.Direct3D9;

namespace DirectionalGlow
{
    public class Hook
    {
        public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            #region Predefined
            private static readonly ReferenceEqualityComparer<T> instance
                = new ReferenceEqualityComparer<T>();
            public static ReferenceEqualityComparer<T> Instance
            {
                get { return instance; }
            }
            #endregion

            public bool Equals(T left, T right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(T value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        static Dictionary<Object, Object> glowmap = new Dictionary<Object, Object>(new ReferenceEqualityComparer<Object>());

        static public void Hook0600065B(Matrix world, Matrix view, Device device, ref float value)
        {
            Matrix local = world * view;
            Vector3 pos = Vector3.TransformCoordinate(new Vector3(), local);
            Vector3 dir = Vector3.TransformNormal(new Vector3(0, 0, 1), local);
            pos.Normalize();
            dir.Normalize();
            value = (float)Math.Pow(Math.Max(Vector3.Dot(pos, dir), 0f), 500);
            device.SetRenderState(RenderState.DestinationBlend, Blend.One);
        }

        static public void Hook06000657(Device device)
        {
            device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        }

        static Device device_global = null;
        static bool in_hook = false;

        static public void Hook06000035<T>(ref T structure, Device device, ref bool transparent)
        {
            in_hook = glowmap.ContainsKey(structure);
            if (in_hook && transparent)
            {
                transparent = false;
                structure = (T)glowmap[structure];
                device.SetRenderState(RenderState.DestinationBlend, Blend.One);
                device.SetRenderState(RenderState.FogEnable, false);
                device_global = device;
            }
            else
            {
                device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
                device.SetRenderState(RenderState.FogEnable, true);
                device_global = null;
            }
        }

        static public void Hook06000715<T, U>(T arg0, U arg1)
        {
            Object[] signal = arg0 as Object[];
            Object[] glow = arg1 as Object[];
            for (int i = 0; i < signal.Length; ++i)
                if (glow[i] != null)
                    if (!glowmap.ContainsKey(signal[i]))
                    glowmap.Add(signal[i], glow[i]);
        }

        static public Material Hook0600003A(Material material)
        {
            if (device_global == null)
                return material;

            Matrix local = device_global.GetTransform(TransformState.World) * device_global.GetTransform(TransformState.View);
            Vector3 pos = Vector3.TransformCoordinate(new Vector3(), local);
            Vector3 dir = Vector3.TransformNormal(new Vector3(0, 0, 1), local);
            pos.Normalize();
            dir.Normalize();
            Color4 color = material.Diffuse;
            color.Alpha = (float)Math.Pow(Math.Max(Vector3.Dot(pos, dir), 0f), 2000);
            material.Diffuse = color;
            return material;
        }

        static public bool Hook0600003E(bool transparent)
        {
            if (in_hook)
                return false;
            return transparent;
        }
    }
}
