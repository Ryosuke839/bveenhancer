using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlimDX;
using SlimDX.DirectSound;
using SlimDX.Multimedia;

namespace SurroundSound
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

        class Buffer3D
        {
            public Vector3? pos = null;
            public SoundBuffer3D[] buf3d;
        };
        static Dictionary<Object, Buffer3D> bufmap = new Dictionary<Object, Buffer3D>(new ReferenceEqualityComparer<Object>());

        public static void Hook0600019B(ref SecondarySoundBuffer[] buf)
        {
            if (buf[0].Capabilities.Control3D)
            {
                Buffer3D buf3d = bufmap.ContainsKey(buf[0]) ? bufmap[buf[0]] : new Buffer3D();
                buf3d.buf3d = new SoundBuffer3D[1];
                buf3d.buf3d[0] = new SoundBuffer3D(buf[0]);
                if (!bufmap.ContainsKey(buf[0]))
                    bufmap.Add(buf[0], buf3d);
            }
        }

        public static void Hook0600019C(ref SecondarySoundBuffer[] buf)
        {
            if (buf[0].Capabilities.Control3D)
            {
                Buffer3D buf3d = bufmap.ContainsKey(buf) ? bufmap[buf] : new Buffer3D();
                buf3d.buf3d = new SoundBuffer3D[buf.Length];
                for (int i = 0; i < buf.Length; ++i)
                    buf3d.buf3d[i] = new SoundBuffer3D(buf[i]);
                if (!bufmap.ContainsKey(buf))
                    bufmap.Add(buf, buf3d);
            }
        }

        static Vector3 cab;
        static float trainlen;

        public static void Hook060002DE(double cx, double cy, double cz, double len)
        {
            cab = new Vector3((float)cx, (float)cy, (float)cz);
            trainlen = (float)len;
        }

        public static void Hook060000E8(string A_0, string A_1, int A_2, DirectSound directsound, ref SecondarySoundBuffer[] array)
        {
            SoundBufferDescription description = default(SoundBufferDescription);
            description.Flags = (BufferFlags.ControlVolume | BufferFlags.ControlFrequency | BufferFlags.Control3D);
            description.AlgorithmFor3D = DirectSound3DAlgorithmGuid.LightHrt3DAlgorithm;
            array = new SecondarySoundBuffer[A_2];
            Buffer3D buf3d = new Buffer3D();
            using (WaveStream waveStream = new WaveStream(A_1))
            {
                description.Format = waveStream.Format;
                int num = 1;
                if (description.Format.Channels > 1)
                {
                    num = (int)description.Format.Channels;
                    description.Format.Channels = 1;
                    description.Format.AverageBytesPerSecond /= num;
                    WaveFormat format = description.Format;
                    format.BlockAlignment /= (short)num;
                }
                description.SizeInBytes = (int)waveStream.Length;
                if (num > 1)
                    description.SizeInBytes /= num;
                byte[] array2 = new byte[(int)waveStream.Length];
                byte[] array3 = new byte[description.SizeInBytes];
                waveStream.Read(array2, 0, (int)waveStream.Length);
                if (num == 1)
                    array3 = array2;
                else
                {
                    switch (description.Format.FormatTag)
                    {
                        case WaveFormatTag.Pcm:
                            switch (description.Format.BitsPerSample)
                            {
                                case 32:
                                    for (int i = 0; i < description.SizeInBytes; i += 4)
                                    {
                                        int num2 = 0;
                                        for (int j = 0; j < num * 4; j += 4)
                                            num2 += BitConverter.ToInt32(array2, i * num + j) / num;
                                        Buffer.BlockCopy(BitConverter.GetBytes(num2), 0, array3, i, 4);
                                    }
                                    break;
                                case 16:
                                    for (int k = 0; k < description.SizeInBytes; k += 2)
                                    {
                                        short num3 = 0;
                                        for (int l = 0; l < num * 2; l += 2)
                                            num3 += (short)((int)BitConverter.ToInt16(array2, k * num + l) / num);
                                        Buffer.BlockCopy(BitConverter.GetBytes(num3), 0, array3, k, 2);
                                    }
                                    break;
                                case 8:
                                    for (int m = 0; m < description.SizeInBytes; m++)
                                    {
                                        byte b = 0;
                                        for (int n = 0; n < num; n++)
                                            b += (byte)((int)array2[m * num + n] / num);
                                        array3[m] = b;
                                    }
                                    break;
                            }
                            break;
                        case WaveFormatTag.IeeeFloat:
                            switch (description.Format.BitsPerSample)
                            {
                                case 32:
                                    for (int i = 0; i < description.SizeInBytes; i += 4)
                                    {
                                        float num2 = 0.0f;
                                        for (int j = 0; j < num * 4; j += 4)
                                            num2 += BitConverter.ToSingle(array2, i * num + j) / num;
                                        Buffer.BlockCopy(BitConverter.GetBytes(num2), 0, array3, i, 4);
                                    }
                                    break;
                            }
                            break;
                    }
                }
                string trunk = A_0.LastIndexOf('.') != -1 ? A_0.Substring(0, A_0.LastIndexOf('.') + 1) : A_0;
                for (int num4 = 0; num4 < A_2; num4++)
                {
                    array[num4] = new SecondarySoundBuffer(directsound, description);
                    array[num4].Write<byte>(array3, 0, LockFlags.None);
                    switch (trunk)
                    {
                        case "lb.":
                        case "cp.":
                        case "bg":
                            buf3d.pos = new Vector3(0.0f, 0.5f, -trainlen * 0.4f) - cab;
                            continue;
                        case "fl.":
                        case "run.":
                            buf3d.pos = new Vector3(0.0f, 0.0f, -trainlen * 0.15f) - cab;
                            continue;
                        case "jt.":
                        case "p.":
                        case "b.":
                        case "rv.":
                        case "ats.":
                            buf3d.pos = new Vector3(0.0f, -0.3f, 0.4f);
                            continue;
                        case "dl.":
                            buf3d.pos = new Vector3(-cab.X - 1.5f, 0.0f, -1.0f);
                            continue;
                        case "hnm":
                        case "hn0.":
                        case "hn1.":
                            buf3d.pos = new Vector3(0.0f, 0.0f, -0.5f) - cab;
                            continue;
                        case "sh":
                        case "bp.":
                        case "m.":
                        case "bc.":
                            buf3d.pos = new Vector3(0.0f, 0.5f, -trainlen * 0.15f) - cab;
                            continue;
                        case "lv.r.":
                            buf3d.pos = new Vector3(1.0f, 1.0f, -trainlen * 0.15f) - cab;
                            continue;

                        case "bz.":
                        case "pl.":
                            buf3d.pos = new Vector3(-0.5f, 0.0f, 0.0f);
                            continue;
                        case "dr.":
                            buf3d.pos = new Vector3(-cab.X + 1.5f, 0.0f, -1.0f);
                            continue;
                        case "lv.l.":
                            buf3d.pos = new Vector3(-1.0f, 1.0f, -trainlen * 0.15f) - cab;
                            continue;
                    }
                    buf3d.pos = new Vector3(0.0f, 0.0f, 0.0f);
                }
            }
            bufmap.Add(array, buf3d);
        }

        public static void Hook06000796(WaveStream waveStream, DirectSound directsound, ref SecondarySoundBuffer buffer)
        {
            SoundBufferDescription description = default(SoundBufferDescription);
            description.Flags = (BufferFlags.ControlVolume | BufferFlags.ControlFrequency | BufferFlags.Control3D);
            description.AlgorithmFor3D = DirectSound3DAlgorithmGuid.LightHrt3DAlgorithm;
            description.Format = waveStream.Format;
            int num2 = 1;
            if (description.Format.Channels > 1 && description.Format.FormatTag == WaveFormatTag.Pcm)
            {
                num2 = (int)description.Format.Channels;
                description.Format.Channels = 1;
                description.Format.AverageBytesPerSecond /= num2;
                WaveFormat format = description.Format;
                format.BlockAlignment /= (short)num2;
            }
            description.SizeInBytes = (int)waveStream.Length;
            if (num2 > 1)
                description.SizeInBytes /= num2;
            buffer = new SecondarySoundBuffer(directsound, description);
            byte[] array = new byte[(int)waveStream.Length];
            waveStream.Read(array, 0, (int)waveStream.Length);
            if (num2 == 1)
                buffer.Write<byte>(array, 0, SlimDX.DirectSound.LockFlags.None);
            else
            {
                byte[] array2 = new byte[description.SizeInBytes];
                switch (description.Format.BitsPerSample)
                {
                    case 32:
                        for (int j = 0; j < description.SizeInBytes; j += 4)
                        {
                            int num3 = 0;
                            for (int k = 0; k < num2 * 4; k += 4)
                                num3 += BitConverter.ToInt32(array, j * num2 + k) / num2;
                            Buffer.BlockCopy(BitConverter.GetBytes(num3), 0, array2, j, 4);
                        }
                        break;
                    case 16:
                        for (int l = 0; l < description.SizeInBytes; l += 2)
                        {
                            short num4 = 0;
                            for (int m = 0; m < num2 * 2; m += 2)
                                num4 += (short)((int)BitConverter.ToInt16(array, l * num2 + m) / num2);
                            Buffer.BlockCopy(BitConverter.GetBytes(num4), 0, array2, l, 2);
                        }
                        break;
                    case 8:
                        for (int n = 0; n < description.SizeInBytes; n++)
                        {
                            byte b = 0;
                            for (int num5 = 0; num5 < num2; num5++)
                                b += (byte)((int)array[n * num2 + num5] / num2);
                            array2[n] = b;
                        }
                        break;
                }
                buffer.Write<byte>(array2, 0, SlimDX.DirectSound.LockFlags.None);
            }
        }

        public static void Hook06000799(WaveStream waveStream, DirectSound directsound, ref SecondarySoundBuffer buffer)
        {
            SoundBufferDescription description = default(SoundBufferDescription);
            description.Flags = (BufferFlags.ControlVolume | BufferFlags.ControlFrequency | BufferFlags.Control3D);
            description.AlgorithmFor3D = DirectSound3DAlgorithmGuid.LightHrt3DAlgorithm;
            description.Format = waveStream.Format;
            Buffer3D buf3d = new Buffer3D();
            int num2 = 1;
            if (description.Format.Channels > 1 && description.Format.FormatTag == WaveFormatTag.Pcm)
            {
                num2 = (int)description.Format.Channels;
                description.Format.Channels = 1;
                description.Format.AverageBytesPerSecond /= num2;
                WaveFormat format = description.Format;
                format.BlockAlignment /= (short)num2;
            }
            description.SizeInBytes = (int)waveStream.Length;
            if (num2 > 1)
                description.SizeInBytes /= num2;
            buffer = new SecondarySoundBuffer(directsound, description);
            byte[] array = new byte[(int)waveStream.Length];
            waveStream.Read(array, 0, (int)waveStream.Length);
            if (num2 == 1)
                buffer.Write<byte>(array, 0, SlimDX.DirectSound.LockFlags.None);
            else
            {
                byte[] array2 = new byte[description.SizeInBytes];
                switch (description.Format.BitsPerSample)
                {
                    case 32:
                        for (int j = 0; j < description.SizeInBytes; j += 4)
                        {
                            int num3 = 0;
                            for (int k = 0; k < num2 * 4; k += 4)
                                num3 += BitConverter.ToInt32(array, j * num2 + k) / num2;
                            Buffer.BlockCopy(BitConverter.GetBytes(num3), 0, array2, j, 4);
                        }
                        break;
                    case 16:
                        for (int l = 0; l < description.SizeInBytes; l += 2)
                        {
                            short num4 = 0;
                            for (int m = 0; m < num2 * 2; m += 2)
                                num4 += (short)((int)BitConverter.ToInt16(array, l * num2 + m) / num2);
                            Buffer.BlockCopy(BitConverter.GetBytes(num4), 0, array2, l, 2);
                        }
                        break;
                    case 8:
                        for (int n = 0; n < description.SizeInBytes; n++)
                        {
                            byte b = 0;
                            for (int num5 = 0; num5 < num2; num5++)
                                b += (byte)((int)array[n * num2 + num5] / num2);
                            array2[n] = b;
                        }
                        break;
                }
                buffer.Write<byte>(array2, 0, SlimDX.DirectSound.LockFlags.None);
            }
            buf3d.pos = new Vector3(0.0f, 1.0f, -5.0f);
            bufmap.Add(buffer, buf3d);
        }

        public static void Hook060001A0(int volspd, ref double vol, Vector3 vel, double voltgt, int eventtimer, SecondarySoundBuffer[] buf, int idx, double pitch, int freqmin, int freqmax, double distmin, Vector3 pos, Matrix rot, Matrix world, Vector3 listvel)
        {
            if (volspd <= 0 || Math.Abs(voltgt - vol) < 1E-05)
                vol = voltgt;
            else
                vol += (voltgt - vol) * (1.0 - Math.Exp(-(double)eventtimer / (double)volspd));
            if (vol < 1E-05)
            {
                buf[idx].Stop();
                return;
            }
            if (vol >= 1.0)
                buf[idx].Volume = 0;
            else
            if (vol > 1E-05)
                buf[idx].Volume = (int)(Math.Log10(vol) * 2000.0);
            else
                buf[idx].Volume = -10000;

            if (bufmap.ContainsKey(buf) || bufmap.ContainsKey(buf[idx]))
            {
                Buffer3D obj = bufmap.ContainsKey(buf) ? bufmap[buf] : bufmap[buf[idx]];
                SoundBuffer3D[] buf3d = obj.buf3d;
                Matrix transformation = world;
                transformation.M41 = 0f;
                transformation.M42 = 0f;
                transformation.M43 = 0f;
                for (int i = 0; i < buf3d.Length; i++)
                {
                    if (obj.pos != null)
                    {
                        buf3d[i].Position = obj.pos.GetValueOrDefault();
                        buf3d[i].MinDistance = buf3d[i].Position.Length();
                    }
                    else
                    {
                        buf3d[i].MinDistance = (float)distmin;
                        buf3d[i].Position = Vector3.TransformCoordinate(pos, rot);
                        buf3d[i].Velocity = Vector3.TransformCoordinate(Vector3.Subtract(vel, listvel), transformation);
                    }
                }
            }
            buf[idx].Frequency = Math.Min(Math.Max((int)(buf[idx].Format.SamplesPerSecond * pitch), freqmin), freqmax);
        }

        public static void Hook060001A7(ref SecondarySoundBuffer[] buf)
        {
            if (bufmap.ContainsKey(buf))
            {
                SoundBuffer3D[] buf3d = bufmap[buf].buf3d;
                for (int i = 0; i < buf3d.Length; i++)
                {
                    if (buf3d[i] != null)
                    {
                        buf3d[i].Dispose();
                        buf3d[i] = null;
                    }
                }
                bufmap.Remove(buf);
            }
            if (bufmap.ContainsKey(buf[0]))
            {
                SoundBuffer3D[] buf3d = bufmap[buf[0]].buf3d;
                for (int i = 0; i < buf3d.Length; i++)
                {
                    if (buf3d[i] != null)
                    {
                        buf3d[i].Dispose();
                        buf3d[i] = null;
                    }
                }
                bufmap.Remove(buf[0]);
            }
        }

        public static void Hook0600087A(ref double front, ref double back, double player, double offset, Vector3 lastpos)
        {
            front = back = Math.Min(Math.Max(player + offset + Math.Abs(lastpos.X) * 0.46630765815, front), back);
        }
    }
}

