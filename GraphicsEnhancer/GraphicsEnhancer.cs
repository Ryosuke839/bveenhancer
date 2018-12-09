using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;

namespace GraphicsEnhancer
{
    public class Hook
    {
        static public void Hook06000005(ref Direct3D d3d, ref Device device, ref PresentParameters param, Control control, bool windowed, Size size, int quality)
        {
            d3d = new Direct3D();
            int adapter = d3d.Adapters.DefaultAdapter.Adapter;
            Capabilities deviceCaps = d3d.GetDeviceCaps(adapter, DeviceType.Hardware);
            DeviceType deviceType = deviceCaps.DeviceType;
            CreateFlags createFlags = (deviceCaps.VertexShaderVersion >= new Version(2, 0)) ? CreateFlags.HardwareVertexProcessing : CreateFlags.SoftwareVertexProcessing;
            param = new PresentParameters();
            param.SwapEffect = SwapEffect.Discard;
            DisplayMode currentDisplayMode = d3d.Adapters[adapter].CurrentDisplayMode;
            param.Windowed = (windowed || !d3d.CheckDeviceType(adapter, DeviceType.Hardware, currentDisplayMode.Format, currentDisplayMode.Format, false));
            if (param.Windowed)
            {
                param.DeviceWindowHandle = control.Handle;
                param.BackBufferWidth = 0;
                param.BackBufferHeight = 0;
            }
            else
            {
                param.BackBufferFormat = currentDisplayMode.Format;
                param.BackBufferCount = 1;
                if (size.Width == 0 || size.Height == 0)
                    size = new Size(currentDisplayMode.Width, currentDisplayMode.Height);
                param.BackBufferWidth = size.Width;
                param.BackBufferHeight = size.Height;
                param.PresentFlags = PresentFlags.LockableBackBuffer;
                control.ClientSize = new Size(currentDisplayMode.Width, currentDisplayMode.Height);
                control.Location = new Point(0, 0);
            }
            if (d3d.CheckDeviceFormat(adapter, DeviceType.Hardware, currentDisplayMode.Format, Usage.DepthStencil, ResourceType.Surface, Format.D24S8))
            {
                param.EnableAutoDepthStencil = true;
                param.AutoDepthStencilFormat = Format.D24S8;
            }
            MultisampleType multisampleType = quality <= 1 ? MultisampleType.None : MultisampleType.NonMaskable;
            while (multisampleType > MultisampleType.None)
            {
                int val;
                int val2;
                if (d3d.CheckDeviceMultisampleType(adapter, deviceType, param.BackBufferFormat, param.Windowed, multisampleType, out val) && d3d.CheckDeviceMultisampleType(adapter, deviceType, Format.D24S8, param.Windowed, multisampleType, out val2))
                {
                    param.Multisample = multisampleType;
                    if (multisampleType == MultisampleType.NonMaskable)
                    {
                        param.MultisampleQuality = Math.Min(Math.Min(val, val2) - 1, (int)Math.Log(quality, 2) - 1);
                        break;
                    }
                    break;
                }
                else
                    multisampleType--;
            }
            param.PresentationInterval = PresentInterval.One;
            device = new Device(d3d, adapter, deviceType, control.Handle, createFlags, new PresentParameters[] { param });
        }

        static public void Hook06000006(Device device)
        {
            device.SetRenderState<Cull>(RenderState.CullMode, Cull.Counterclockwise);
            device.SetTextureStageState(0, TextureStage.ColorArg1, TextureArgument.Diffuse);
            device.SetTextureStageState(0, TextureStage.ColorArg2, TextureArgument.Texture);
            device.SetTextureStageState(0, TextureStage.ColorOperation, TextureOperation.Modulate);
            device.SetTextureStageState(0, TextureStage.AlphaArg1, TextureArgument.Diffuse);
            device.SetTextureStageState(0, TextureStage.AlphaArg2, TextureArgument.Texture);
            device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate);
            device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Anisotropic);
            device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Anisotropic);
            device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Anisotropic);
            Capabilities deviceCaps = device.Direct3D.GetDeviceCaps(0, DeviceType.Hardware);
            device.SetSamplerState(0, SamplerState.MaxAnisotropy, deviceCaps.MaxAnisotropy);
            device.SetRenderState<Compare>(RenderState.ZFunc, Compare.LessEqual);
            device.SetRenderState<ZBufferType>(RenderState.ZEnable, ZBufferType.UseZBuffer);
            device.SetRenderState(RenderState.AlphaTestEnable, true);
            device.SetRenderState(RenderState.AlphaRef, 3);
            device.SetRenderState<Compare>(RenderState.AlphaFunc, Compare.Greater);
            device.SetRenderState(RenderState.AlphaBlendEnable, true);
            device.SetRenderState<Blend>(RenderState.SourceBlend, Blend.SourceAlpha);
            device.SetRenderState<Blend>(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            device.SetRenderState<BlendOperation>(RenderState.BlendOperation, BlendOperation.Add);
            device.EnableLight(0, true);
        }

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

        static void ApplyFilter(ref Texture tex)
        {
            if (tex == null)
                return;
            if (tex.LevelCount == 0)
                return;

            if (tex.GetLevelDescription(0).Format == Format.Dxt1)
            {
                Surface oldsuf = tex.GetSurfaceLevel(0);
                Texture newtex = new Texture(tex.Device, oldsuf.Description.Width, oldsuf.Description.Height, tex.LevelCount, oldsuf.Description.Usage, Format.Dxt3, oldsuf.Description.Pool);
                Surface newsuf = newtex.GetSurfaceLevel(0);
                Surface.FromSurface(newsuf, oldsuf, Filter.None, 0);
                newsuf.Dispose();
                oldsuf.Dispose();
                tex.Dispose();
                tex = newtex;
            }
            switch (tex.GetLevelDescription(0).Format)
            {
                case Format.A8R8G8B8:
                case Format.A1R5G5B5:
                case Format.A4R4G4B4:
                case Format.A8:
                case Format.A8R3G3B2:
                case Format.A2B10G10R10:
                case Format.A8B8G8R8:
                case Format.A2R10G10B10:
                case Format.A16B16G16R16:
                case Format.A8P8:
                case Format.A8L8:
                case Format.A4L4:
                case Format.A2W10V10U10:
                case Format.A16B16G16R16F:
                case Format.A32B32G32R32F:
                case Format.A1:
                case Format.Dxt2:
                case Format.Dxt3:
                case Format.Dxt4:
                case Format.Dxt5:
                    Surface lower = null;
                    for (int j = 0; j < tex.LevelCount; j++)
                    {
                        Surface upperlevel = tex.GetSurfaceLevel(j);
                        Surface upper = Surface.CreateOffscreenPlain(tex.Device, upperlevel.Description.Width, upperlevel.Description.Height, Format.A8R8G8B8, Pool.SystemMemory);

                        if (j == 0)
                            Surface.FromSurface(upper, upperlevel, Filter.Default, 0);

                        DataRectangle upperrect = upper.LockRectangle(j == 0 ? LockFlags.None : LockFlags.Discard);
                        int upperpitch = upperrect.Pitch;
                        if (j != 0)
                        {
                            DataRectangle lowerrect = lower.LockRectangle(LockFlags.ReadOnly);
                            int lowerpitch = lowerrect.Pitch;
                            
                            unsafe
                            {
                                byte* lowerptr = (byte*)lowerrect.Data.DataPointer.ToPointer();
                                byte* upperptr = (byte*)upperrect.Data.DataPointer.ToPointer();
                                for (int k = 0; k < (lower.Description.Height + 1) / 2; k++)
                                {
                                    int mc = k == (lower.Description.Height + 1) / 2 - 1 && lower.Description.Height % 2 == 1 ? 1 : 2;
                                    for (int l = 0; l < (lower.Description.Width + 1) / 2; l++)
                                    {
                                        int nc = l == (lower.Description.Width + 1) / 2 - 1 && lower.Description.Width % 2 == 1 ? 1 : 2;
                                        int r = 0, g = 0, b = 0, a = 0;
                                        for (int m = 0; m < mc; m++)
                                        {
                                            for (int n = 0; n < nc; n++)
                                            {
                                                byte* src = lowerptr + (k * 2 + m) * lowerpitch + (l * 2 + n) * 4;
                                                r += src[0] * src[3];
                                                g += src[1] * src[3];
                                                b += src[2] * src[3];
                                                a += src[3];
                                            }
                                        }
                                        if (a == 0)
                                            a = 1;
                                        byte* dest = upperptr + k * upperpitch + l * 4;
                                        dest[0] = (byte)(r / a);
                                        dest[1] = (byte)(g / a);
                                        dest[2] = (byte)(b / a);
                                        dest[3] = (byte)(a / (nc * mc));
                                    }
                                }
                            }
                            lower.UnlockRectangle();
                            lower.Dispose();
                        }
                        
                        unsafe
                        {
                            byte* upperptr = (byte*)upperrect.Data.DataPointer.ToPointer();
                            for (int k = 0; k < upper.Description.Height; k++)
                            {
                                for (int l = 0; l < upper.Description.Width; l++)
                                {
                                    if (upperptr[k * upperpitch + l * 4 + 3] > 0)
                                        continue;
                                    int r = 0, g = 0, b = 0, a = 0;
                                    for (int m = -1; m < 2; m++)
                                    {
                                        byte* src = upperptr + (k + m + upper.Description.Height) % upper.Description.Height * upperpitch + l * 4;
                                        r += src[0] * src[3];
                                        g += src[1] * src[3];
                                        b += src[2] * src[3];
                                        a += src[3];
                                    }
                                    for (int n = -1; n < 2; n++)
                                    {
                                        byte* src = upperptr + k * upperpitch + (l + n + upper.Description.Width) % upper.Description.Width * 4;
                                        r += src[0] * src[3];
                                        g += src[1] * src[3];
                                        b += src[2] * src[3];
                                        a += src[3];
                                    }
                                    if (a == 0)
                                    {
                                        for (int m = -1; m < 2; m += 2)
                                        {
                                            for (int n = -1; n < 2; n += 2)
                                            {
                                                byte* src = upperptr + (k + m + upper.Description.Height) % upper.Description.Height * upperpitch + (l + n + upper.Description.Width) % upper.Description.Width * 4;
                                                r += src[0] * src[3];
                                                g += src[1] * src[3];
                                                b += src[2] * src[3];
                                                a += src[3];
                                            }
                                        }
                                    }
                                    if (a == 0)
                                        continue;
                                    byte* dest = upperptr + k * upperpitch + l * 4;
                                    dest[0] = (byte)(r / a);
                                    dest[1] = (byte)(g / a);
                                    dest[2] = (byte)(b / a);
                                }
                            }
                        }

                        upper.UnlockRectangle();
                        lower = upper;
                        Surface.FromSurface(upperlevel, upper, Filter.Default, 0);
                        upperlevel.Dispose();
                    }
                    lower.Dispose();
                    tex.AddDirtyRectangle();
                    break;
            }
        }

        class RCTexture
        {
            String name;
            Texture tex;
            public int refcount;

            public static Dictionary<String, RCTexture> strmap = new Dictionary<String, RCTexture>();
            public static Dictionary<Texture, RCTexture> texmap = new Dictionary<Texture, RCTexture>(new ReferenceEqualityComparer<Texture>());

            private RCTexture(String name, Texture tex)
            {
                this.name = name;
                this.tex = tex;
                this.refcount = 0;
            }

            public static Texture Get(Device device, string fileName, int width, int height, int levelCount, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
            {
                RCTexture rctex;
                if (!strmap.ContainsKey(fileName))
                {
                    rctex = new RCTexture(fileName, Texture.FromFile(device, fileName, width, height, levelCount, usage, format, pool, filter, Filter.Default, colorKey));
                    ApplyFilter(ref rctex.tex);
                    strmap[fileName] = rctex;
                    texmap[rctex.tex] = rctex;
                }
                else
                    rctex = strmap[fileName];
                ++rctex.refcount;
                return rctex.tex;
            }

            public static void Dispose(Texture tex)
            {
                if (tex == null)
                    return;
                if (texmap.ContainsKey(tex))
                {
                    RCTexture rctex = texmap[tex];
                    if (--rctex.refcount > 0)
                        return;
                    strmap.Remove(rctex.name);
                    texmap.Remove(tex);
                }
                tex.Dispose();
            }
        };

        public static void Hook060000F8(string name, Control.ControlCollection collection, SortedList<string, string> dict)
        {
            if (dict.ContainsKey("config.filter") && (dict["config.filter"] == "テクスチャー フィルター" || dict["config.filter"] == "アンチエイリアス"))
                dict["config.filter"] = "アンチエイリアス";
            else
                dict["config.filter"] = "Antialiasing";
            if (dict.ContainsKey("config.anisotropy") && (dict["config.anisotropy"] == "異方性フィルター: {0}" || dict["config.anisotropy"] == "品質: {0}"))
                dict["config.anisotropy"] = "品質: {0}";
            else
                dict["config.anisotropy"] = "Quality: {0}";
            if (dict.ContainsKey("config.fine") && (dict["config.fine"] == "細かい" || dict["config.fine"] == "滑らか"))
                dict["config.fine"] = "滑らか";
            else
                dict["config.fine"] = "Smooth";

            foreach (object obj in collection)
            {
                Control control = (Control)obj;
                string key = name + control.Text;
                if (dict.ContainsKey(key))
                    control.Text = dict[key];
                if (control is ToolStrip)
                    Hook060000F9(name, ((ToolStrip)control).Items, dict);
                else
                    Hook060000F8(name, control.Controls, dict);
            }
        }

        public static void Hook060000F9(string name, ToolStripItemCollection collection, SortedList<string, string> dict)
        {
            foreach (object obj in collection)
            {
                ToolStripItem toolStripItem = (ToolStripItem)obj;
                string key = name + toolStripItem.Text;
                if (dict.ContainsKey(key))
                    toolStripItem.Text = dict[key];
            }
        }

        public static void Hook06000092(Direct3D d3d, TrackBar trackbar, int quality, bool windowed)
        {
            int adapter = d3d.Adapters.DefaultAdapter.Adapter;
            int val;
            int val2;
            int maxquality;
            if (d3d.CheckDeviceMultisampleType(adapter, DeviceType.Hardware, d3d.Adapters[adapter].CurrentDisplayMode.Format, windowed, MultisampleType.NonMaskable, out val) &&
                d3d.CheckDeviceMultisampleType(adapter, DeviceType.Hardware, Format.D24S8, windowed, MultisampleType.NonMaskable, out val2))
                maxquality = Math.Min(val, val2);
            else
                maxquality = 0;
            Capabilities deviceCaps = d3d.GetDeviceCaps(0, DeviceType.Hardware);
            trackbar.Enabled = (maxquality > 0);
            if (trackbar.Enabled)
            {
                trackbar.Maximum = maxquality;
                try
                {
                    trackbar.Value = (int)Math.Log(quality, 2);
                }
                catch
                {
                }
            }
        }

        public static void Hook0600009A(Label restart)
        {
            restart.Visible = true;
        }

        public static void Hook0600003D(Texture tex, ref bool trans, ref Material mat)
        {
            trans = false;
            if (mat.Specular.Red == mat.Specular.Green && mat.Specular.Blue == mat.Specular.Alpha)
                return;
            if (tex == null)
                return;
            if (tex.LevelCount <= 0)
                return;
            Surface suf = tex.GetSurfaceLevel(tex.LevelCount - 1);
            Surface read = Surface.CreateOffscreenPlain(tex.Device, suf.Description.Width, suf.Description.Height, Format.A8R8G8B8, Pool.SystemMemory);
            Surface.FromSurface(read, suf, Filter.None, 0);
            suf.Dispose();
            DataRectangle rect = read.LockRectangle(LockFlags.ReadOnly);
            int pitch = rect.Pitch;
            unsafe
            {
                byte* ptr = (byte*)rect.Data.DataPointer.ToPointer();
                for (int k = 0; k < read.Description.Height; k++)
                {
                    for (int l = 0; l < read.Description.Width; l++)
                    {
                        if (ptr[k * pitch + l * 4 + 3] < 255)
                        {
                            trans = true;
                            break;
                        }
                    }
                    if (trans)
                        break;
                }
            }
            read.UnlockRectangle();
            read.Dispose();
        }

        public static Texture Hook0600002D(Device device, string fileName, int width, int height, int levelCount, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
        {
            return RCTexture.Get(device, fileName, width, height, levelCount, usage, format, pool, filter, Filter.Default, colorKey);
        }

        public static void Hook0600002E(Mesh mesh, int i, ref Material mat)
        {
            mat = mesh.GetMaterials()[i].MaterialD3D;
            mat.Ambient = mat.Diffuse;
            Color4 col = new Color4(0.0f, 0.0f, 0.0f, 0.0f);

            if ((mesh.VertexFormat & VertexFormat.Texture1) != VertexFormat.None)
            {
                int index = 0;
                int count = mesh.VertexCount;
                if (mesh.GetAttributeTable() != null && i < mesh.GetAttributeTable().Length)
                {
                    AttributeRange attr = mesh.GetAttributeTable()[i];
                    if (attr != null)
                    {
                        index = attr.VertexStart;
                        count = attr.VertexCount;
                    }
                }

                int stride = mesh.VertexBuffer.Description.SizeInBytes / mesh.VertexCount;
                count = Math.Min(count, mesh.VertexCount - index);
                if (count > 0 && mesh.VertexBuffer != null)
                {
                    DataStream stream = mesh.VertexBuffer.Lock(stride * index, stride * count, LockFlags.ReadOnly);
                    for (int j = 0; j < count; ++j)
                    {
                        stream.Seek(stride - 8, SeekOrigin.Current);
                        Vector2 vec = stream.Read<Vector2>();
                        col.Red = Math.Min(col.Red, vec.X);
                        col.Green = Math.Max(col.Green, vec.X);
                        col.Blue = Math.Min(col.Blue, vec.Y);
                        col.Alpha = Math.Max(col.Alpha, vec.Y);
                    }
                    mesh.VertexBuffer.Unlock();
                }
            }

            mat.Specular = col;
        }

        public static Result Hook06000035(BaseMesh obj, int subset)
        {
            Color4 col = obj.Device.Material.Specular;
            obj.Device.SetSamplerState(0, SamplerState.AddressU, 0.0f <= col.Red && col.Green <= 1.0f ? TextureAddress.Clamp : TextureAddress.Wrap);
            obj.Device.SetSamplerState(0, SamplerState.AddressV, 0.0f <= col.Blue && col.Alpha <= 1.0f ? TextureAddress.Clamp : TextureAddress.Wrap);
            Result res = obj.DrawSubset(subset);
            obj.Device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Wrap);
            obj.Device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Wrap);
            return res;
        }

        public static Texture Hook06000030(Device device, string fileName, int width, int height, int levelCount, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
        {
            return RCTexture.Get(device, fileName, width, height, levelCount, usage, format, pool, filter, Filter.Default, colorKey);
        }

        public static void Hook06000032(ComObject obj)
        {
            RCTexture.Dispose(obj as Texture);
        }

        public static Matrix Hook060001F8(float x, float y, float z)
        {
            return Matrix.Translation(x - 0.5f, y + 0.5f, z);
        }
    }
}