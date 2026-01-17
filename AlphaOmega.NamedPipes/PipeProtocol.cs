using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;

namespace AlphaOmega.NamedPipes
{
	internal static class PipeProtocol
	{
		public static async Task WriteMessageAsync(Stream stream, PipeMessage message, CancellationToken token)
		{
			Console.WriteLine($"Writing message: {message.ToString()}");

			Byte[] data = PipeMessage.Serialize(message);
			Byte[] length = BitConverter.GetBytes(data.Length);

			await stream.WriteAsync(length,0, length.Length, token);
			await stream.WriteAsync(data, 0, data.Length, token);
			await stream.FlushAsync(token);
		}

		public static async Task<PipeMessage> ReadMessageAsync(Stream stream, CancellationToken token)
		{
			Byte[] lengthBuffer = new Byte[4];
			await ReadExactlyAsync(stream, lengthBuffer, token);

			Int32 length = BitConverter.ToInt32(lengthBuffer, 0);
			if(length <= 0)
				throw new InvalidDataException("Invalid message length");

			Byte[] payload = new Byte[length];
			await ReadExactlyAsync(stream, payload, token);

			PipeMessage result = PipeMessage.Deserialize<PipeMessage>(payload);
			Console.WriteLine($"Received message: {result.ToString()}");
			return result;
		}

		private static async Task ReadExactlyAsync(Stream stream, Byte[] buffer, CancellationToken token)
		{
			Int32 offset = 0;
			while(offset < buffer.Length)
			{
				Int32 read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, token);
				if(read == 0)
					throw new EndOfStreamException("Unexpected end of stream");
				offset += read;
			}
		}
	}
}