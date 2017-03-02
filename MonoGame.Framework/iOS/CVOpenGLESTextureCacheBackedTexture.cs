using System;
using System.Diagnostics.Contracts;
using System.Threading;
using CoreVideo;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenGLES;
using OpenTK.Graphics.ES20;

namespace Microsoft.Xna.Framework2
{
    public class CVOpenGLESTextureCacheBackedTexture : Texture2D
    {
        public static CVOpenGLESTextureCacheBackedTexture InstanceLuma = null;
        public static CVOpenGLESTextureCacheBackedTexture InstanceChroma = null;
        // Texture2D _mgTexture;
        static CVOpenGLESTextureCache videoTextureCache;
        internal CVOpenGLESTexture _cvGLTexure;
        static object locker = new object ();
        public static int FrameNumber = -1;

        public CVOpenGLESTextureCacheBackedTexture (GraphicsDevice device) : base (device, 1, 1)//width height set automatically as texture data set
        {
        }

        /// <summary>
        /// Only continue if lock succeeds
        /// </summary>
        /// <returns>The lock.</returns>
        public static bool Lock ()
        {
            Monitor.Enter (locker);
            if (InstanceLuma == null) {
                Monitor.Exit (locker);
                return false;
            }

            GL.Flush ();
            if (InstanceLuma._cvGLTexure != null && InstanceChroma._cvGLTexure != null) {

                //luma and chroma always bound to 0/1
                GL.ActiveTexture (TextureUnit.Texture0);
                GL.BindTexture (InstanceLuma._cvGLTexure.Target, InstanceLuma._cvGLTexure.Name);
                GL.ActiveTexture (TextureUnit.Texture1);
                GL.BindTexture (InstanceChroma._cvGLTexure.Target, InstanceChroma._cvGLTexure.Name);
                //  GL.Flush ();

                //luma->chroma
            }
            return true;
        }

        public static void Unlock ()
        {
            //  GL.Flush ();
            Monitor.Exit (locker);
        }

        public static void Recycle ()
        {
            if (InstanceLuma._cvGLTexure != null) {
                InstanceLuma._cvGLTexure.Dispose ();
            }

            if (InstanceChroma._cvGLTexure != null) {
                InstanceChroma._cvGLTexure.Dispose ();
            }

            if (videoTextureCache != null) {
                videoTextureCache.Flush (CVOptionFlags.None);
            }
            //  GL.Flush ();
        }

        public void SetTexture (CVImageBuffer pixelBuffer, int textureWidth, int textureHeight, All internalFormat, All pixelFormat, int plane)
        {
            if (videoTextureCache == null) {
                videoTextureCache = new CVOpenGLESTextureCache (EAGLContext.CurrentContext);
            }

            CVReturn status;
            var previousBoundVBuffer = GraphicsExtensions.GetBoundTexture2D ();

            if (videoTextureCache == null) {
                return;
            }

            _cvGLTexure = videoTextureCache.TextureFromImage (pixelBuffer, true, internalFormat, textureWidth, textureHeight, pixelFormat, DataType.UnsignedByte, plane, out status);

            GL.BindTexture (_cvGLTexure.Target, _cvGLTexure.Name);
            GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);


            this.glTexture = _cvGLTexure.Name;
            this._levelCount = 0;
            //incorrect but ignore for now
            this.glFormat = PixelFormat.Alpha;
            this.glInternalFormat = PixelInternalFormat.Alpha;

            GL.BindTexture (TextureTarget.Texture2D, previousBoundVBuffer);
            width = textureWidth;
            height = textureHeight;

            glLastSamplerState = null;//hack do next time the state wil be set
        }

        public static void BindTextures ()
        {
            if (InstanceLuma._cvGLTexure != null && InstanceChroma._cvGLTexure != null) {
                InstanceLuma.glTextureUnit = TextureUnit.Texture0 + 1;
                InstanceChroma.glTextureUnit = TextureUnit.Texture0 + 0;
                GL.BindTexture (InstanceLuma._cvGLTexure.Target, InstanceLuma._cvGLTexure.Name);
                GL.BindTexture (InstanceChroma._cvGLTexure.Target, InstanceChroma._cvGLTexure.Name);

            }
        }

        protected override void Dispose (bool disposing)
        {
            // base.Dispose (disposing); dont call base, because it will try to dispose based on our handles. But we manage these ourselves!
            lock (locker) {
                if (InstanceLuma != null) {
                    if (InstanceLuma._cvGLTexure != null) {
                        InstanceLuma._cvGLTexure.Dispose ();
                    }
                }
                InstanceLuma = null;

                if (InstanceChroma != null) {
                    if (InstanceChroma._cvGLTexure != null) {
                        InstanceChroma._cvGLTexure.Dispose ();
                    }
                }
                InstanceChroma = null;
                if (videoTextureCache != null)
                    videoTextureCache.Dispose ();
                videoTextureCache = null;
            }
        }
    }
}
