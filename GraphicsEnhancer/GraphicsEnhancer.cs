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
            if (tex != null)
            {
                if (tex.GetLevelDescription(0).Format == Format.A8R8G8B8)
                {
                    for (int j = 0; j < tex.LevelCount - 1; j++)
                    {
                        Surface surfaceLevel = tex.GetSurfaceLevel(j);
                        Surface surfaceLevel2 = tex.GetSurfaceLevel(j + 1);
                        DataRectangle dataRectangle = surfaceLevel.LockRectangle(LockFlags.ReadOnly);
                        DataRectangle dataRectangle2 = surfaceLevel2.LockRectangle(LockFlags.Discard);
                        DataStream data = dataRectangle.Data;
                        DataStream data2 = dataRectangle2.Data;
                        for (int k = 0; k < surfaceLevel2.Description.Height; k++)
                        {
                            for (int l = 0; l < surfaceLevel2.Description.Width; l++)
                            {
                                uint[] array = new uint[4];
                                for (int m = 0; m < 2; m++)
                                {
                                    for (int n = 0; n < 2; n++)
                                    {
                                        byte[] array2 = new byte[4];
                                        data.Read(array2, 0, 4);
                                        array[0] += (uint)(array2[3] * array2[0]);
                                        array[1] += (uint)(array2[3] * array2[1]);
                                        array[2] += (uint)(array2[3] * array2[2]);
                                        array[3] += (uint)array2[3];
                                    }
                                    data.Seek((m == 0) ? ((long)(dataRectangle.Pitch - 8)) : (-(long)dataRectangle.Pitch), SeekOrigin.Current);
                                }
                                if (array[3] == 0u)
                                    array[3] = 1u;
                                array[0] /= array[3];
                                array[1] /= array[3];
                                array[2] /= array[3];
                                array[3] /= 4u;
                                data2.Write(Array.ConvertAll<uint, byte>(array, (uint c) => (byte)c), 0, 4);
                            }
                            data.Seek((long)(dataRectangle.Pitch * 2 - surfaceLevel2.Description.Width * 8), SeekOrigin.Current);
                            data2.Seek((long)(dataRectangle2.Pitch - surfaceLevel2.Description.Width * 4), SeekOrigin.Current);
                        }
                        surfaceLevel.UnlockRectangle();
                        surfaceLevel2.UnlockRectangle();
                        surfaceLevel.Dispose();
                        surfaceLevel2.Dispose();
                    }
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
    }
}