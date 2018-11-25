using System;
using System.Collections.Generic;
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

        static public void Hook0600003D(ref Texture field, Texture tex)
        {
            if (tex != null && tex.LevelCount > 0)
            {
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
                        Surface lowerlevel = tex.GetSurfaceLevel(0);
                        Surface lower = Surface.CreateOffscreenPlain(tex.Device, lowerlevel.Description.Width, lowerlevel.Description.Height, Format.A8R8G8B8, Pool.SystemMemory);
                        Surface.FromSurface(lower, lowerlevel, Filter.Default, 0);
                        lowerlevel.Dispose();
                        for (int j = 1; j < tex.LevelCount; j++)
                        {
                            DataRectangle lowerrect = lower.LockRectangle(LockFlags.ReadOnly);
                            DataStream lowerdata = lowerrect.Data;
                            int lowerpitch = lowerrect.Pitch;
                            Surface upperlevel = tex.GetSurfaceLevel(j);
                            Surface upper = Surface.CreateOffscreenPlain(tex.Device, upperlevel.Description.Width, upperlevel.Description.Height, Format.A8R8G8B8, Pool.SystemMemory);
                            DataRectangle upperrect = upper.LockRectangle(LockFlags.Discard);
                            DataStream upperdata = upperrect.Data;
                            int upperpitch = upperrect.Pitch;
                            uint[] array = new uint[4];
                            byte[] array2 = new byte[4];
                            byte[] array3 = new byte[4];
                            for (int k = 0; k < (lower.Description.Height + 1) / 2; k++)
                            {
                                int mc = k == (lower.Description.Height + 1) / 2 - 1 && lower.Description.Height % 2 == 1 ? 1 : 2;
                                for (int l = 0; l < (lower.Description.Width + 1) / 2; l++)
                                {
                                    int nc = l == (lower.Description.Width + 1) / 2 - 1 && lower.Description.Width % 2 == 1 ? 1 : 2;
                                    array[0] = array[1] = array[2] = array[3] = 0;
                                    for (int m = 0; m < mc; m++)
                                    {
                                        for (int n = 0; n < nc; n++)
                                        {
                                            lowerdata.Read(array2, 0, 4);
                                            array[0] += (uint)(array2[3] * array2[0]);
                                            array[1] += (uint)(array2[3] * array2[1]);
                                            array[2] += (uint)(array2[3] * array2[2]);
                                            array[3] += (uint)array2[3];
                                        }
                                        if (mc > 1)
                                            lowerdata.Seek((m == 0) ? ((long)(lowerpitch - nc * 4)) : (-(long)lowerpitch), SeekOrigin.Current);
                                    }
                                    if (array[3] == 0u)
                                        array[3] = 1u;
                                    array[0] /= array[3];
                                    array[1] /= array[3];
                                    array[2] /= array[3];
                                    array[3] /= 4u;
                                    array3[0] = (byte)array[0];
                                    array3[1] = (byte)array[1];
                                    array3[2] = (byte)array[2];
                                    array3[3] = (byte)array[3];
                                    upperdata.Write(array3, 0, 4);
                                }
                                if (k == (lower.Description.Height + 1) / 2 - 1)
                                    break;
                                lowerdata.Seek((long)(lowerpitch * 2 - lower.Description.Width * 4), SeekOrigin.Current);
                                upperdata.Seek((long)(upperpitch - (lower.Description.Width + 1) / 2 * 4), SeekOrigin.Current);
                            }
                            lower.UnlockRectangle();
                            lower.Dispose();
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
            field = tex;
        }

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

        public static Texture Hook0600002D(Device device, string fileName, int width, int height, int levelCount, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
        {
            return Texture.FromFile(device, fileName, width, height, levelCount, usage, format, pool, filter, Filter.Default, colorKey);
        }

        public static Texture Hook06000030(Device device, string fileName, int width, int height, int levelCount, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
        {
            return Texture.FromFile(device, fileName, width, height, levelCount, usage, format, pool, filter, Filter.Default, colorKey);
        }
    }
}