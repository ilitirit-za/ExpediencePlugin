using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Expedience.Utils
{
	public interface IEncryptor
	{
		string Encrypt(string data);
	}

	internal class Encryptor : IEncryptor
    {
        private readonly RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();

		public Encryptor()
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Expedience.Resources.key.secret");
			using var reader = new StreamReader(stream);
			string base64String = reader.ReadToEnd().Trim();

			byte[] cspBlob = Convert.FromBase64String(base64String);

			_rsa.ImportCspBlob(cspBlob);
		}

		public string Encrypt(string data)
        {
            var dataToEncrypt = Encoding.UTF8.GetBytes(data);
            // Set the maximum data size for encryption
            int maxDataSize = (_rsa.KeySize / 8) - 11;

            // Split the data into chunks
            byte[] encryptedData = new byte[0];
            for (int i = 0; i < dataToEncrypt.Length; i += maxDataSize)
            {
                int remainingBytes = dataToEncrypt.Length - i;
                int dataSize = remainingBytes > maxDataSize ? maxDataSize : remainingBytes;
                byte[] chunk = new byte[dataSize];
                Array.Copy(dataToEncrypt, i, chunk, 0, dataSize);

                // Encrypt the chunk of data
                byte[] encryptedChunk = _rsa.Encrypt(chunk, false);
                encryptedData = encryptedData.Concat(encryptedChunk).ToArray();
            }

            return Convert.ToBase64String(encryptedData);
        }
    }
}
