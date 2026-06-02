using QRCoder;
using System;

namespace BloodDonationApp.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        return Convert.ToBase64String(qrCodeBytes);
                    }
                }
            }
        }
    }
}
