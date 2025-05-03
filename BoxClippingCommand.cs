using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using OpenTK.Graphics.OpenGL4;
using BoxClipping.OpenGL;
using BoxClipping.OpenTK;
using System.IO;
using Rhino.Render.ChangeQueue;


namespace BoxClipping
{
    public class BoxClippingCommand : Command
    {
        public BoxClippingCommand()
        {
            Instance = this;
        }

        public static BoxClippingCommand Instance { get; private set; }

        public override string EnglishName => "BoxClippingCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var conduit = new BoxClippingConduit();
            conduit.Enabled = true;
            doc.Views.Redraw();

            return Result.Success;
        }





        public class BoxClippingConduit : DisplayConduit
        {
            private readonly Guid _boxId;
            private bool _glLoaded = false;
            private int _program;
            private int _vbo;
            private int _vao;
            private int _uBoxMinLocation;
            private int _uBoxMaxLocation;
            private DateTime _startTime = DateTime.Now;



            protected override void DrawForeground(DrawEventArgs e)
            {

                if (!DisplayPipeline.MakeDefaultOpenGLContextCurrent())
                {
                    RhinoApp.WriteLine("❌ GL context not available.");
                    return;
                }

                if (!_glLoaded)
                {
                    GL.LoadBindings(new RhinoOpenTKBindings());

                    // シェーダープログラムをロード
                    string baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string shaderDir = Path.Combine(baseDir, "shaders");

                    string vertexPath = Path.Combine(shaderDir, "vertex.glsl");
                    string fragmentPath = Path.Combine(shaderDir, "fragment.glsl");

                    _program = ShaderLoader.LoadShaderProgram(vertexPath, fragmentPath);

                    //頂点
                    float[] vertices = new float[]
                    {
                        -1f, -1f,  // 左下
                         1f, -1f,  // 右下
                         1f,  1f,  // 右上
                        -1f,  1f   // 左上
                    };

                    uint[] indices = { 0, 1, 2, 2, 3, 0 };


                    GL.GenVertexArrays(1, out _vao);
                    GL.BindVertexArray(_vao);

                    GL.GenBuffers(1, out _vbo);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
                    GL.EnableVertexAttribArray(0);

                    _glLoaded = true;
                }
                GL.UseProgram(_program);

                float time = (float)(DateTime.Now - _startTime).TotalSeconds;

                int timeLocation = GL.GetUniformLocation(_program, "uTime");
                if (timeLocation != -1)
                    GL.Uniform1(timeLocation, time);

                var uResLocation = GL.GetUniformLocation(_program, "uResolution");
                if (uResLocation != -1)
                {
                    var viewportSize = e.Viewport.Size;
                    GL.Uniform2(uResLocation, (float)viewportSize.Width, (float)viewportSize.Height);
                }
                else
                {
                    RhinoApp.WriteLine("⚠️ uResolution uniform not found.");
                }


                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                GL.BindVertexArray(_vao);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            }
        }
    }
}
