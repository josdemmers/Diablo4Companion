using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace D4Companion.SystemPresets
{
    public sealed class MonitorDuplicator : IDisposable
    {
        private readonly IDXGIAdapter1 _adapter;
        private readonly uint _adapterIndex;
        private readonly IDXGIOutput1 _output;
        private readonly uint _outputIndex;
        private readonly ID3D11Device? _device;
        private readonly ID3D11DeviceContext? _context;
        private readonly IDXGIOutputDuplication _duplication;        

        // Start of Constructors region

        #region Constructors

        public MonitorDuplicator(IDXGIFactory1 factory, uint adapterIndex, uint outputIndex)
        {
            _adapterIndex = adapterIndex;
            _outputIndex = outputIndex;

            factory.EnumAdapters1(adapterIndex, out _adapter).CheckError();

            FeatureLevel[] levels =
            {
                FeatureLevel.Level_12_2,
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            D3D11.D3D11CreateDevice(_adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, levels, out _device).CheckError();

            _context = _device?.ImmediateContext;
            _adapter.EnumOutputs(outputIndex, out IDXGIOutput output).CheckError();
            _adapter.Dispose();

            var desc = output.Description;
            if (!desc.AttachedToDesktop)
                throw new InvalidOperationException("Output is not a desktop-attached monitor.");

            _output = output.QueryInterface<IDXGIOutput1>();
            _duplication = _output.DuplicateOutput(_device);
        }

        #endregion

        // Start of Properties region

        #region Properties

        public ID3D11Device? Device => _device;

        public string DeviceName => _output.Description.DeviceName;

        #endregion

        // Start of Methods region

        #region Methods

        public string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string ReplaceInvalidChars(string filename, string replacement)
        {
            return string.Join(replacement, filename.Split(Path.GetInvalidFileNameChars()));
        }

        public override string ToString()
        {
            string deviceName = _output.Description.DeviceName;
            deviceName = RemoveInvalidChars(deviceName);

            return deviceName;
        }

        public void Dispose()
        {
            _duplication?.Dispose();
            _output?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
            _adapter?.Dispose();
        }        

        public BitmapSource? TryGetScreen()
        {
            var result = _duplication.AcquireNextFrame(1000, out var frameInfo, out var dskTopResource);
            if (result.Failure)
            {
                dskTopResource?.Dispose();
                _duplication.ReleaseFrame();
                return null;
            }

            using var frameTexture = dskTopResource.QueryInterface<ID3D11Texture2D>();
            var textureDesc = new Texture2DDescription
            {
                CPUAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = frameTexture.Description.Width,
                Height = frameTexture.Description.Height,
                MiscFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            using var currentFrame = _device.CreateTexture2D(textureDesc);
            using var desktopResource = dskTopResource;

            _device.ImmediateContext.CopyResource(currentFrame, frameTexture);
            var dataBox = _device.ImmediateContext.Map(currentFrame, 0);

            int stride = (int)frameTexture.Description.Width * 4; // BGRA32
            byte[] pixels = new byte[stride * frameTexture.Description.Height];

            unsafe
            {
                fixed (byte* pDest = pixels)
                {
                    byte* pSrc = (byte*)dataBox.DataPointer;

                    for (int y = 0; y < frameTexture.Description.Height; y++)
                    {
                        Buffer.MemoryCopy(pSrc + y * dataBox.RowPitch, pDest + y * stride, stride, stride);
                    }
                }
            }

            _duplication.ReleaseFrame();
            _device.ImmediateContext.Unmap(currentFrame, 0);

            var bitmapSource = BitmapSource.Create((int)frameTexture.Description.Width, (int)frameTexture.Description.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);

            return bitmapSource;
        }

        #endregion
    }
}