using System;
using Windows.Media.MediaProperties;

namespace Inspelning.Recorder.Utils
{
    public class StreamPropertiesHelper
    {
        public StreamPropertiesHelper(IMediaEncodingProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // This helper class only uses VideoEncodingProperties or VideoEncodingProperties
            if (properties is not ImageEncodingProperties && properties is not VideoEncodingProperties)
            {
                throw new ArgumentException("Argument is of the wrong type. Required: " +
                                            nameof(ImageEncodingProperties)
                                            + " or " + nameof(VideoEncodingProperties) + ".", nameof(properties));
            }

            // Store the actual instance of the IMediaEncodingProperties for setting them later
            EncodingProperties = properties;
        }

        public uint Width => EncodingProperties is VideoEncodingProperties properties ? properties.Width : 0;

        public uint Height => EncodingProperties is VideoEncodingProperties properties ? properties.Height : 0;

        public uint FrameRate
        {
            get
            {
                if (EncodingProperties is not VideoEncodingProperties properties)
                {
                    return 0;
                }

                if (properties.FrameRate.Denominator != 0)
                {
                    return properties.FrameRate.Numerator /
                           properties.FrameRate.Denominator;
                }

                return 0;
            }
        }

        public double AspectRatio => Math.Round(Height != 0 ? Width / (double)Height : double.NaN, 2);

        public IMediaEncodingProperties EncodingProperties { get; }

        public string GetFriendlyName(bool showFrameRate = true)
        {
            if (EncodingProperties is ImageEncodingProperties ||
                !showFrameRate)
            {
                return Width + "x" + Height + " [" + AspectRatio + "] " + EncodingProperties.Subtype;
            }

            if (EncodingProperties is VideoEncodingProperties)
            {
                return Width + "x" + Height + " [" + AspectRatio + "] " + FrameRate + "FPS " +
                       EncodingProperties.Subtype;
            }

            return string.Empty;
        }
    }
}