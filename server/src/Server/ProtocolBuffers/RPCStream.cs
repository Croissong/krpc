using System.IO;
using Google.Protobuf;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCStream : Message.RPCStream
    {
        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (Response value)
        {
            var message = value.ToProtobufResponse ();
            var bufferStream = new MemoryStream ();
            message.WriteDelimitedTo (bufferStream);
            Stream.Write (bufferStream.ToArray ());
        }

        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            try {
                var codedStream = new CodedInputStream (data, offset, length);
                // Get the protobuf message size
                int size = (int)codedStream.ReadUInt32 ();
                int totalSize = (int)codedStream.Position + size;
                // Check if enough data is available, if not then delay the decoding
                if (length < totalSize)
                    throw new NoRequestException ();
                // Decode the request
                request = Schema.KRPC.Request.Parser.ParseFrom (codedStream).ToRequest ();
                return totalSize;
            } catch (InvalidProtocolBufferException e) {
                throw new MalformedRequestException (e.Message);
            }
        }
    }
}